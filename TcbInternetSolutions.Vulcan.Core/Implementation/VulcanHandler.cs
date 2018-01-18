namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction.RuntimeModel.Internal;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using Extensions;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Default Vulcan handler
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanHandler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanHandler : IVulcanHandler
    {
        /// <summary>
        /// Vulcan logger
        /// </summary>
        protected static ILogger Logger = LogManager.GetLogger(typeof(VulcanHandler));

        /// <summary>
        /// List of vulcan clients
        /// </summary>
        protected Dictionary<CultureInfo, IVulcanClient> clients = new Dictionary<CultureInfo, IVulcanClient>();

        private Dictionary<Type, List<IVulcanConditionalContentIndexInstruction>> conditionalContentIndexInstructions;
        private readonly IVulcanPipelineSelector _VulcanPipelineSelector;
        private readonly IEnumerable<IVulcanPipelineInstaller> _VulcanPipelineInstallers;
        private object lockObject = new object();

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanIndexingModifiers"></param>
        /// <param name="vulcanClientConnectionSettings"></param>
        /// <param name="contentLoader"></param>
        /// <param name="vulcanCreateIndexCustomizer"></param>
        /// <param name="vulcanPipelineSelector"></param>
        /// <param name="vulcanPipelineInstallers"></param>
        public VulcanHandler
        (
            IEnumerable<IVulcanIndexingModifier> vulcanIndexingModifiers,
            IVulcanClientConnectionSettings vulcanClientConnectionSettings,
            IContentLoader contentLoader,
            IVulcanCreateIndexCustomizer vulcanCreateIndexCustomizer,
            IVulcanPipelineSelector vulcanPipelineSelector,
            IEnumerable<IVulcanPipelineInstaller> vulcanPipelineInstallers
        )
        {
            IndexingModifers = vulcanIndexingModifiers;
            CommonConnectionSettings = vulcanClientConnectionSettings;
            ContentLoader = contentLoader;
            CreateIndexCustomizer = vulcanCreateIndexCustomizer;            
            _VulcanPipelineSelector = vulcanPipelineSelector;
            _VulcanPipelineInstallers = vulcanPipelineInstallers;

            conditionalContentIndexInstructions = new Dictionary<Type, List<IVulcanConditionalContentIndexInstruction>>();
        }

        /// <summary>
        /// Deleted indices handler
        /// </summary>
        public IndexDeleteHandler DeletedIndices { get; set; }

        /// <summary>
        /// Index name
        /// </summary>
        public virtual string Index => CommonConnectionSettings.Index;

        /// <summary>
        /// Indexing modifiers
        /// </summary>
        public virtual IEnumerable<IVulcanIndexingModifier> IndexingModifers { get; }
        /// <summary>
        /// Inected connection settings
        /// </summary>
        protected IVulcanClientConnectionSettings CommonConnectionSettings { get; set; }

        /// <summary>
        /// Injected content loader
        /// </summary>
        protected IContentLoader ContentLoader { get; set; }

        /// <summary>
        /// Injected create index customizer
        /// </summary>
        protected IVulcanCreateIndexCustomizer CreateIndexCustomizer { get; }

        /// <summary>
        /// Adds index instruction
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instruction"></param>
        public void AddConditionalContentIndexInstruction<T>(Func<T, bool> instruction) where T : IContent
        {
            if (!conditionalContentIndexInstructions.ContainsKey(typeof(T)))
            {
                conditionalContentIndexInstructions.Add(typeof(T), new List<IVulcanConditionalContentIndexInstruction>());
            }

            conditionalContentIndexInstructions[typeof(T)].Add(new VulcanConditionalContentIndexInstruction<T>(instruction));
        }

        /// <summary>
        /// Determines if content can be indexed
        /// </summary>
        /// <param name="objectToIndex"></param>
        /// <returns></returns>
        public bool AllowContentIndexing(IContent objectToIndex)
        {
            bool allowIndex = true; // default is true;

            foreach (var kvp in conditionalContentIndexInstructions)
            {
                if (kvp.Key.IsAssignableFrom(objectToIndex.GetType()))
                {
                    foreach (var instruction in kvp.Value)
                    {
                        allowIndex = instruction.AllowContentIndexing(objectToIndex);

                        if (!allowIndex) break; // we only care about first FALSE
                    }
                }
            }

            return allowIndex;
        }

        /// <summary>
        /// Delete content by language
        /// </summary>
        /// <param name="content"></param>
        public virtual void DeleteContentByLanguage(IContent content)
        {
            IVulcanClient client;

            if (!(content is ILocalizable))
            {
                client = GetClient(CultureInfo.InvariantCulture);

            }
            else
            {
                client = GetClient((content as ILocalizable).Language);
            }

            client.DeleteContent(content);
        }

        /// <summary>
        /// Delete content for all clients
        /// </summary>
        /// <param name="contentLink"></param>
        public virtual void DeleteContentEveryLanguage(ContentReference contentLink)
        {
            // we don't know what language(s), or even if invariant, so send a delete request to all

            foreach (var client in GetClients())
            {
                client.DeleteContent(contentLink);
            }
        }

        /// <summary>
        /// Delete index
        /// </summary>
        public virtual void DeleteIndex()
        {
            lock (lockObject)
            {
                var client = CreateElasticClient(CommonConnectionSettings.ConnectionSettings); // use a raw elasticclient because we just need this to be quick
                var indices = client.CatIndices();

                if (indices != null && indices.Records != null && indices.Records.Any())
                {
                    var indicesToDelete = new List<string>();

                    foreach (var index in indices.Records.Where(i => i.Index.StartsWith(Index + "_")).Select(i => i.Index))
                    {
                        var response = client.DeleteIndex(index);

                        if (!response.IsValid)
                        {
                            Logger.Error("Could not run a delete index: " + response.DebugInformation);
                        }
                        else
                        {
                            indicesToDelete.Add(index);
                        }
                    }

                    DeletedIndices?.Invoke(indicesToDelete);
                }

                // todo: this is a temp fix to keep multiple templates from getting added, shouldn't exist long term....
                if (client.IndexTemplateExists("analyzer_disabling").Exists)
                {
                    // clean up template that was too generic in a shared environment
                    client.DeleteIndexTemplate("analyzer_disabling");
                }

                clients = new Dictionary<CultureInfo, IVulcanClient>(); // need to force a re-creation                
            }
        }

        /// <summary> 
        /// Get a Vulcan client
        /// </summary>
        /// <param name="language">Pass in null for current culture, a specific culture or CultureInfo.InvariantCulture to get a client for non-language specific data</param>
        /// <returns>A Vulcan client</returns>
        public virtual IVulcanClient GetClient(CultureInfo language = null)
        {
            var cultureInfo = language ?? CultureInfo.CurrentUICulture;

            if (clients.TryGetValue(cultureInfo, out IVulcanClient storedClient))
                return storedClient;

            lock (lockObject)
            {
                // we now know what our culture is (current culture or invariant), but we need to choose the language analyzer                
                var languageAnalyzer = VulcanHelper.GetAnalyzer(cultureInfo);
                var indexName = VulcanHelper.GetIndexName(Index, cultureInfo);
                var settings = CommonConnectionSettings.ConnectionSettings;
                settings.InferMappingFor<ContentMixin>(pd => pd.Ignore(p => p.MixinInstance));
                settings.DefaultIndex(indexName);

                var client = CreateVulcanClient(Index, settings, cultureInfo);

                // first let's check our version
                var nodesInfo = client.NodesInfo();

                if (nodesInfo?.Nodes?.Any() != true)
                {
                    throw new Exception("Could not get Nodes info to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                }
                else
                {
                    var node = nodesInfo.Nodes.First(); // just use first

                    if (string.IsNullOrWhiteSpace(node.Value.Version)) // just use first
                    {
                        throw new Exception("Could not find a version on node to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                    }
                    else
                    {
                        if (node.Value.Version.StartsWith("1."))
                        {
                            throw new Exception("Sorry, Vulcan only works with Elasticsearch version 2.x or higher. The Elasticsearch node you are currently connected to is version " + node.Value.Version);
                        }
                    }
                }

                client.RunCustomIndexTemplates(Index, Logger);

                // keep our base last with lowest possible Order
                client.PutIndexTemplate($"{Index}_analyzer_disabling", ad => ad
                        .Order(0)
                        .Template($"{Index}*") //match on all created indices for index name
                        .Mappings(mappings => mappings.Map("_default_", map => map.DynamicTemplates(
                            dyn => dyn.DynamicTemplate("analyzer_template", dt => dt
                                .Match("*") //matches all fields
                                .MatchMappingType("string") //that are a string
                                .Mapping(dynmap => dynmap.String(s => s
                                    .NotAnalyzed()
                                    .IgnoreAbove(CreateIndexCustomizer.IgnoreAbove) // needed for: document contains at least one immense term in field
                                    .IncludeInAll(false)
                                    .Fields(f => f
                                        .String(ana => ana
                                            .Name(VulcanFieldConstants.AnalyzedModifier)
                                            .IncludeInAll(false)
                                            .Store(true)
                                        )
                                    ))
                                )
                            )))));

                // todo: nest 5 to 2 difference
                // note: strings are no more in ES5, for not analyzed text use Keyword and for analyzed use Text
                // keep our base last with lowest possible Order
                //client.PutIndexTemplate($"{Index}_analyzer_disabling", ad => ad
                //        .Order(0)
                //        .Template($"{Index}*") //match on all created indices for index name
                //        .Mappings(mappings => mappings.Map("_default_", map => map.DynamicTemplates(
                //            dyn => dyn.DynamicTemplate("analyzer_template", dt => dt
                //                .Match("*") //matches all fields
                //                .MatchMappingType("string") //that are a string
                //                .Mapping(dynmap => dynmap.Keyword(s => s
                //                    .IgnoreAbove(CreateIndexCustomizer.IgnoreAbove) // needed for: document contains at least one immense term in field
                //                    .IncludeInAll(false)
                //                    .Fields(f => f
                //                        .Text(ana => ana
                //                            .Name(VulcanFieldConstants.AnalyzedModifier)
                //                            .IncludeInAll(false)
                //                            .Store(true)
                //                        )
                //                    ))
                //                )
                //            )))));

                if (!client.IndexExists(indexName).Exists)
                {
                    var response = client.CreateIndex(indexName, CreateIndexCustomizer.CustomizeIndex);

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not create index " + indexName + ": " + response.DebugInformation);
                    }
                }

                client.Refresh(indexName);
                var closeResponse = client.CloseIndex(indexName);

                if (!closeResponse.IsValid)
                {
                    Logger.Error("Could not close index " + indexName + ": " + closeResponse.DebugInformation);
                }

                InitializeAnalyzer(client);

                // run installers
                foreach(var installer in _VulcanPipelineInstallers)
                {
                    installer.Install(client);
                }

                client.RunCustomizers(Logger); // allows for customizations

                var openResponse = client.OpenIndex(indexName);

                // todo: nest 5 to 2 difference
                var initShards = client.ClusterHealth(x => x.WaitForActiveShards(CreateIndexCustomizer.WaitForActiveShards)); // fixes empty results on first request                
                //var initShards = client.ClusterHealth(x => x.WaitForActiveShards(CreateIndexCustomizer.WaitForActiveShards.ToString())); // fixes empty results on first request

                clients[cultureInfo] = client;

                return client;
            }
        }

        /// <summary>
        /// Gets all vulcan clients
        /// </summary>
        /// <returns></returns>
        public virtual IVulcanClient[] GetClients()
        {
            var clients = new List<IVulcanClient>();
            var client = CreateElasticClient(CommonConnectionSettings.ConnectionSettings);
            var indices = client.CatIndices();

            if (indices != null && indices.Records != null && indices.Records.Any())
            {
                foreach (var index in indices.Records.Where(i => i.Index.StartsWith(Index + "_")).Select(i => i.Index))
                {
                    var cultureName = index.Substring(Index.Length + 1);

                    clients.Add(GetClient(cultureName.Equals("invariant", StringComparison.InvariantCultureIgnoreCase) ? CultureInfo.InvariantCulture : new CultureInfo(cultureName)));
                }
            }

            return clients.ToArray();
        }

        /// <summary>
        /// Index content for language
        /// </summary>
        /// <param name="content"></param>
        public virtual void IndexContentByLanguage(IContent content)
        {
            IVulcanClient client;

            if (!(content is ILocalizable))
            {
                client = GetClient(CultureInfo.InvariantCulture);
            }
            else
            {
                client = GetClient((content as ILocalizable).Language);
            }

            client.IndexContent(content);
        }

        /// <summary>
        /// Index content for all langauges
        /// </summary>
        /// <param name="content"></param>
        public virtual void IndexContentEveryLanguage(IContent content)
        {
            if (!(content is ILocalizable))
            {
                var client = GetClient(CultureInfo.InvariantCulture);

                client.IndexContent(content);
            }
            else
            {
                foreach (var language in (content as ILocalizable).ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.IndexContent(ContentLoader.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language));
                }
            }
        }

        /// <summary>
        /// Index content for all languages
        /// </summary>
        /// <param name="contentLink"></param>
        public virtual void IndexContentEveryLanguage(ContentReference contentLink)
        {
            if (!ContentReference.IsNullOrEmpty(contentLink))
            {
                var content = ContentLoader.Get<IContent>(contentLink);

                if (content != null) IndexContentEveryLanguage(content);
            }
        }

        /// <summary>
        /// Gets an elastic client
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IElasticClient CreateElasticClient(IConnectionSettingsValues settings) => new ElasticClient(settings);

        /// <summary>
        /// Gets a vulcan client
        /// </summary>
        /// <param name="index"></param>
        /// <param name="settings"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        protected virtual IVulcanClient CreateVulcanClient(string index, ConnectionSettings settings, CultureInfo culture) =>
            new VulcanClient(index, settings, culture, ContentLoader, this, _VulcanPipelineSelector);

        /// <summary>
        /// Get elision articles
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        protected virtual string[] GetElisionArticles(string language)
        {
            switch (language)
            {
                case "catalan":
                    return new string[] { "d", "l", "m", "n", "s", "t" };

                case "french":
                    return new string[] { "l", "m", "t", "qu", "n", "s", "j", "d", "c", "jusqu", "quoiqu", "lorsqu", "puisqu" };

                case "irish":
                    return new string[] { "h", "n", "t" };

                case "italian":
                    return new string[] { "c", "l", "all", "dall", "dell", "nell", "sull", "coll", "pell", "gl", "agl", "dagl", "degl", "negl", "sugl", "un", "m", "t", "s", "v", "d" };

                default:
                    return new string[] { "" };
            }
        }

        /// <summary>
        /// Get filters for language
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        protected virtual string[] GetFilters(string language)
        {
            switch (language)
            {
                case "arabic":
                    return new string[] { "lowercase", "synonyms", "stop", "arabic_normalization", "stemmer" };

                case "catalan":
                case "french":
                case "italian":
                    return new string[] { "elision", "lowercase", "synonyms", "stop", "stemmer" };

                case "cjk":
                    return new string[] { "cjk_width", "lowercase", "cjk_bigram", "synonyms", "stop" };

                case "dutch":
                    return new string[] { "lowercase", "synonyms", "stop", "override", "stemmer" };

                case "english":
                    return new string[] { "possessive", "lowercase", "synonyms", "stop", "stemmer" };

                case "german":
                    return new string[] { "lowercase", "synonyms", "stop", "german_normalization", "stemmer" };

                case "greek":
                    return new string[] { "custom_lowercase", "synonyms", "stop", "stemmer" };

                case "hindi":
                    return new string[] { "lowercase", "indic_normalization", "hindi_normalization", "synonyms", "stop", "stemmer" };

                case "irish":
                    return new string[] { "stop", "elision", "custom_lowercase", "synonyms", "stemmer" };

                case "persian":
                    return new string[] { "lowercase", "arabic_normalization", "persian_normalization", "synonyms", "stop" };

                case "sorani":
                    return new string[] { "sorani_normalization", "lowercase", "synonyms", "stop", "stemmer" };

                case "turkish":
                    return new string[] { "apostrophe", "custom_lowercase", "synonyms", "stop", "stemmer" };

                case "thai":
                    return new string[] { "lowercase", "synonyms", "stop" };

                case "standard":
                    return new string[] { "lowercase", "synonyms" };

                default:
                    return new string[] { "lowercase", "synonyms", "stop", "stemmer" };
            }
        }

        /// <summary>
        /// Get stemmer language
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        protected virtual string GetStemmerLanguage(string language)
        {
            switch (language)
            {
                case "french":
                    return "light_french";

                case "german":
                    return "light_german";

                case "italian":
                    return "light_italian";

                case "portuguese":
                    return "light_portuguese";

                case "spanish":
                    return "light_spanish";

                default:
                    return language;
            }
        }

        /// <summary>
        /// Get stopwords
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        protected virtual string GetStopwordsLanguage(string language)
        {
            switch (language)
            {
                case "cjk":
                    return "english";

                default:
                    return language;
            }
        }

        /// <summary>
        /// Get synonyms
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual string[] GetSynonyms(IVulcanClient client)
        {
            var resolved = new List<string>();

            var synonyms = client.GetSynonyms();

            if (synonyms != null)
            {
                foreach (var synonym in synonyms)
                {
                    if (synonym.Value.Value) // bidirectional
                    {
                        resolved.Add(synonym.Key + "," + string.Join(",", synonym.Value.Key));
                    }
                    else
                    {
                        resolved.Add(synonym.Key + " => " + string.Join(",", synonym.Value.Key));
                    }
                }
            }

            if (resolved.Count == 0)
            {
                resolved.Add("thisisadummyterm,makesurethesynonymsenabled");
            }

            return resolved.ToArray();
        }

        /// <summary>
        /// Initialize analyzer on elasticsearch
        /// </summary>
        /// <param name="client"></param>
        protected virtual void InitializeAnalyzer(IVulcanClient client)
        {
            var language = VulcanHelper.GetAnalyzer(client.Language);
            IUpdateIndexSettingsResponse response;

            if (language != "standard")
            {
                // first, stop words

                response = client.UpdateIndexSettings(client.IndexName, uix => uix
                    .IndexSettings(ixs => ixs
                        .Analysis(ana => ana
                            .TokenFilters(tf => tf
                                .Stop("stop", sw => sw
                                    .StopWords("_" + GetStopwordsLanguage(language) + "_"))))));

                if (!response.IsValid)
                {
                    Logger.Error("Could not set up stop words for " + client.IndexName + ": " + response.DebugInformation);
                }

                // next, stemmer

                if (!(new string[] { "cjk", "persian", "thai" }.Contains(language)))
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Stemmer("stemmer", stm => stm
                                        .Language(GetStemmerLanguage(language)))))));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not set up stemmers for " + client.IndexName + ": " + response.DebugInformation);
                    }
                }

                // next, stemmer overrides

                if (language == "dutch")
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .StemmerOverride("override", stm => stm
                                        .Rules("fiets=>fiets",
                                                "bromfiets=>bromfiets",
                                                "ei=>eier",
                                                "kind=>kinder"))))));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not set up stemmer overrides for " + client.IndexName + ": " + response.DebugInformation);
                    }
                }

                // next, elision

                if (new string[] { "catalan", "french", "irish", "italian" }.Contains(language))
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Elision("elision", e => e
                                        .Articles(GetElisionArticles(language)))))));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not set up elisions for " + client.IndexName + ": " + response.DebugInformation);
                    }
                }

                // next, possessive

                if (language == "english")
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Stemmer("possessive", stm => stm
                                        .Language("possessive_english"))))));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not set up possessives for " + client.IndexName + ": " + response.DebugInformation);
                    }
                }

                // next, lowercase

                if (new string[] { "greek", "irish", "turkish" }.Contains(language))
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Lowercase("custom_lowercase", stm => stm
                                        .Language(language))))));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not set up lowercases for " + client.IndexName + ": " + response.DebugInformation);
                    }
                }
            }

            response = client.UpdateIndexSettings(client.IndexName, uix => uix
                .IndexSettings(ixs => ixs
                    .Analysis(ana => ana
                        .TokenFilters(tf => tf
                            .Synonym("synonyms", syn => syn
                                .Synonyms(GetSynonyms(client))))
                        .Analyzers(a => a
                            .Custom("default", cad => cad
                                .Tokenizer("standard")
                                .Filters(GetFilters(language)))))));

            if (!response.IsValid)
            {
                Logger.Error("Could not set up custom analyzers for " + client.IndexName + ": " + response.DebugInformation);
            }

            if (language == "persian")
            {
                response = client.UpdateIndexSettings(client.IndexName, uix => uix
                    .IndexSettings(ixs => ixs
                        .Analysis(ana => ana
                            .CharFilters(cf => cf
                                .Mapping("zero_width_spaces", stm => stm
                                    .Mappings("\\u200C=> ")))
                            .Analyzers(a => a
                                .Custom("default", cad => cad
                                    .CharFilters("zero_width_spaces"))))));

                if (!response.IsValid)
                {
                    Logger.Error("Could not set up char filters for " + client.IndexName + ": " + response.DebugInformation);
                }
            }
        }
    }
}