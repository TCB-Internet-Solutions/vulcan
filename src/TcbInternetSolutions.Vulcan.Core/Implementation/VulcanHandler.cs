using System.Collections.Concurrent;

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
        /// Vulcan logger, uses getter to avoid lock issues
        /// </summary>
        protected static ILogger Logger => LogManager.GetLogger(typeof(VulcanHandler));

        /// <summary>
        /// List of vulcan clients
        /// </summary>
        protected ConcurrentDictionary<CultureInfo, IVulcanClient> Clients = new ConcurrentDictionary<CultureInfo, IVulcanClient>();

        private readonly Dictionary<Type, List<IVulcanConditionalContentIndexInstruction>> _conditionalContentIndexInstructions;
        private readonly IVulcanPipelineSelector _vulcanPipelineSelector;
        private readonly IEnumerable<IVulcanPipelineInstaller> _vulcanPipelineInstallers;
        private readonly object _lockObject = new object();

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
            _vulcanPipelineSelector = vulcanPipelineSelector;
            _vulcanPipelineInstallers = vulcanPipelineInstallers;

            _conditionalContentIndexInstructions = new Dictionary<Type, List<IVulcanConditionalContentIndexInstruction>>();
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
            var typeT = typeof(T);

            if (!_conditionalContentIndexInstructions.ContainsKey(typeT))
            {
                _conditionalContentIndexInstructions.Add(typeT, new List<IVulcanConditionalContentIndexInstruction>());
            }

            _conditionalContentIndexInstructions[typeT].Add(new VulcanConditionalContentIndexInstruction<T>(instruction));
        }

        /// <summary>
        /// Determines if content can be indexed
        /// </summary>
        /// <param name="objectToIndex"></param>
        /// <returns></returns>
        public bool AllowContentIndexing(IContent objectToIndex)
        {
            var allowIndex = true; // default is true;

            foreach (var kvp in _conditionalContentIndexInstructions)
            {
                if (!kvp.Key.IsInstanceOfType(objectToIndex)) continue;

                foreach (var instruction in kvp.Value)
                {
                    allowIndex = instruction.AllowContentIndexing(objectToIndex);

                    if (!allowIndex) break; // we only care about first FALSE
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

            if (content is ILocalizable localizable)
            {
                client = GetClient(localizable.Language);

            }
            else
            {
                client = GetClient(CultureInfo.InvariantCulture);
            }

            client.DeleteContent(content);
        }

        /// <summary>
        /// Delete content for all clients
        /// </summary>
        /// <param name="contentLink"></param>
        /// <param name="typeName"></param>
        public virtual void DeleteContentEveryLanguage(ContentReference contentLink, string typeName)
        {
            // we don't know what language(s), or even if invariant, so send a delete request to all
            foreach (var client in GetClients())
            {
                client.DeleteContent(contentLink, typeName);
            }
        }

        /// <summary>
        /// Delete index
        /// </summary>
        public virtual void DeleteIndex()
        {
            lock (_lockObject)
            {
                var client = CreateElasticClient(CommonConnectionSettings.ConnectionSettings); // use a raw elasticclient because we just need this to be quick
                var indices = client.CatIndices();

                if (indices?.Records?.Any() == true)
                {
                    var indicesToDelete = new List<string>();

                    foreach (var index in indices.Records.Where(i => i.Index.StartsWith($"{Index}_")).Select(i => i.Index))
                    {
                        var response = client.DeleteIndex(index);

                        if (!response.IsValid)
                        {
                            Logger.Error($"Could not run a delete index: {response.DebugInformation}");
                        }
                        else
                        {
                            indicesToDelete.Add(index);
                        }
                    }

                    DeletedIndices?.Invoke(indicesToDelete);
                }

                Clients.Clear(); // need to force a re-creation                
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

            if (Clients.TryGetValue(cultureInfo, out var storedClient))
                return storedClient;

            lock (_lockObject)
            {
                // todo: need some sort of check here to make sure we still need to create a client

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

                var node = nodesInfo.Nodes.First(); // just use first

                if (string.IsNullOrWhiteSpace(node.Value.Version)) // just use first
                {
                    throw new Exception("Could not find a version on node to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                }

                if (node.Value.Version.StartsWith("1."))
                {
                    throw new Exception("Sorry, Vulcan only works with Elasticsearch version 2.x or higher. The Elasticsearch node you are currently connected to is version " + node.Value.Version);
                }

                client.RunCustomIndexTemplates(Index, Logger);

                // keep our base last with lowest possible Order

#if NEST2                
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
                                            .Store()
                                        )
                                    ))
                                )
                            )))));
#elif NEST5
                // note: strings are no more in ES5, for not analyzed text use Keyword and for analyzed use Text
                client.PutIndexTemplate($"{Index}_analyzer_disabling", ad => ad
                        .Order(0)
                        .Template($"{Index}*") //match on all created indices for index name
                        .Mappings(mappings => mappings.Map("_default_", map => map.DynamicTemplates(
                            dyn => dyn.DynamicTemplate("analyzer_template", dt => dt
                                .Match("*") //matches all fields
                                .MatchMappingType("string") //that are a string
                                .Mapping(dynmap => dynmap.Keyword(s => s
                                    .IgnoreAbove(CreateIndexCustomizer.IgnoreAbove) // needed for: document contains at least one immense term in field
                                    .Fields(f => f
                                        .Text(ana => ana
                                            .Name(VulcanFieldConstants.AnalyzedModifier)
                                            .Store()
                                        )
                                    ))
                                )
                            )))));
#endif
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
                foreach (var installer in _vulcanPipelineInstallers)
                {
                    installer.Install(client);
                }

                // allows for customizations
                client.RunCustomizers(Logger); 
                client.RunCustomMappers(Logger);

                client.OpenIndex(indexName);

                if (CreateIndexCustomizer.WaitForActiveShards > 0)
                {
                    // Init shards to attempt to fix empty results on first request
                    client.ClusterHealth(x => x.WaitForActiveShards(
#if NEST2
                        CreateIndexCustomizer.WaitForActiveShards
#elif NEST5
                        CreateIndexCustomizer.WaitForActiveShards.ToString()
#endif
                    ));
                }

                storedClient = client;
            }

            Clients[cultureInfo] = storedClient;

            return storedClient;
        }

        /// <summary>
        /// Gets all vulcan clients
        /// </summary>
        /// <returns></returns>
        public virtual IVulcanClient[] GetClients()
        {
            var clientList = new List<IVulcanClient>();
            var client = CreateElasticClient(CommonConnectionSettings.ConnectionSettings);
            var indices = client.CatIndices();

            if (indices?.Records?.Any() != true) return clientList.ToArray();

            clientList.AddRange
            (
                indices.Records
                    .Where(i => i.Index.StartsWith(Index + "_")).Select(i => i.Index)
                    .Select(index => index.Substring(Index.Length + 1))
                    .Select(cultureName =>
                        GetClient(cultureName.Equals("invariant", StringComparison.OrdinalIgnoreCase) ?
                            CultureInfo.InvariantCulture :
                            new CultureInfo(cultureName)
                        )
                    )
            );

            return clientList.ToArray();
            
        }

        /// <summary>
        /// Index content for language
        /// </summary>
        /// <param name="content"></param>
        public virtual void IndexContentByLanguage(IContent content)
        {
            IVulcanClient client;

            if (content is ILocalizable localizable)
            {
                client = GetClient(localizable.Language);
            }
            else
            {
                client = GetClient(CultureInfo.InvariantCulture);
            }

            client.IndexContent(content);
        }

        /// <summary>
        /// Index content for all langauges
        /// </summary>
        /// <param name="content"></param>
        public virtual void IndexContentEveryLanguage(IContent content)
        {
            if (content is ILocalizable localizable)
            {
                foreach (var language in localizable.ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.IndexContent(ContentLoader.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language));
                }
            }
            else
            {
                var client = GetClient(CultureInfo.InvariantCulture);

                client.IndexContent(content);
            }
        }

        /// <summary>
        /// Index content for all languages
        /// </summary>
        /// <param name="contentLink"></param>
        public virtual void IndexContentEveryLanguage(ContentReference contentLink)
        {
            if (ContentReference.IsNullOrEmpty(contentLink)) return;

            var content = ContentLoader.Get<IContent>(contentLink);

            if (content != null) IndexContentEveryLanguage(content);
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
            new VulcanClient(index, settings, culture, ContentLoader, this, _vulcanPipelineSelector);

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
                    return new[] { "d", "l", "m", "n", "s", "t" };

                case "french":
                    return new[] { "l", "m", "t", "qu", "n", "s", "j", "d", "c", "jusqu", "quoiqu", "lorsqu", "puisqu" };

                case "irish":
                    return new[] { "h", "n", "t" };

                case "italian":
                    return new[] { "c", "l", "all", "dall", "dell", "nell", "sull", "coll", "pell", "gl", "agl", "dagl", "degl", "negl", "sugl", "un", "m", "t", "s", "v", "d" };

                default:
                    return new[] { "" };
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
                    return new[] { "lowercase", "synonyms", "stop", "arabic_normalization", "stemmer" };

                case "catalan":
                case "french":
                case "italian":
                    return new[] { "elision", "lowercase", "synonyms", "stop", "stemmer" };

                case "cjk":
                    return new[] { "cjk_width", "lowercase", "cjk_bigram", "synonyms", "stop" };

                case "dutch":
                    return new[] { "lowercase", "synonyms", "stop", "override", "stemmer" };

                case "english":
                    return new[] { "possessive", "lowercase", "synonyms", "stop", "stemmer" };

                case "german":
                    return new[] { "lowercase", "synonyms", "stop", "german_normalization", "stemmer" };

                case "greek":
                    return new[] { "custom_lowercase", "synonyms", "stop", "stemmer" };

                case "hindi":
                    return new[] { "lowercase", "indic_normalization", "hindi_normalization", "synonyms", "stop", "stemmer" };

                case "irish":
                    return new[] { "stop", "elision", "custom_lowercase", "synonyms", "stemmer" };

                case "persian":
                    return new[] { "lowercase", "arabic_normalization", "persian_normalization", "synonyms", "stop" };

                case "sorani":
                    return new[] { "sorani_normalization", "lowercase", "synonyms", "stop", "stemmer" };

                case "turkish":
                    return new[] { "apostrophe", "custom_lowercase", "synonyms", "stop", "stemmer" };

                case "thai":
                    return new[] { "lowercase", "synonyms", "stop" };

                case "standard":
                    return new[] { "lowercase", "synonyms" };

                default:
                    return new[] { "lowercase", "synonyms", "stop", "stemmer" };
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
        protected virtual IEnumerable<string> GetStopwordsLanguage(string language)
        {
            var stopWord = language;

            switch (language)
            {
                case "cjk":
                    stopWord = "english";
                    break;
            }

            return new[] { $"_{stopWord}_" };
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
                                    .StopWords(GetStopwordsLanguage(language)))))));

                if (!response.IsValid)
                {
                    Logger.Error($"Could not set up stop words for {client.IndexName}: {response.DebugInformation}");
                }

                // next, stemmer
                if (!new[] { "cjk", "persian", "thai" }.Contains(language))
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Stemmer("stemmer", stm => stm
                                        .Language(GetStemmerLanguage(language)))))));

                    if (!response.IsValid)
                    {
                        Logger.Error($"Could not set up stemmers for {client.IndexName}: {response.DebugInformation}");
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
                        Logger.Error($"Could not set up stemmer overrides for {client.IndexName}: {response.DebugInformation}");
                    }
                }

                // next, elision
                if (new[] { "catalan", "french", "irish", "italian" }.Contains(language))
                {
                    response = client.UpdateIndexSettings(client.IndexName, uix => uix
                        .IndexSettings(ixs => ixs
                            .Analysis(ana => ana
                                .TokenFilters(tf => tf
                                    .Elision("elision", e => e
                                        .Articles(GetElisionArticles(language)))))));

                    if (!response.IsValid)
                    {
                        Logger.Error($"Could not set up elisions for {client.IndexName}: {response.DebugInformation}");
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
                        Logger.Error($"Could not set up possessives for {client.IndexName}: {response.DebugInformation}");
                    }
                }

                // next, lowercase
                if (new[] { "greek", "irish", "turkish" }.Contains(language))
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
                Logger.Error($"Could not set up custom analyzers for {client.IndexName}: {response.DebugInformation}");
            }

            if (language != "persian") return;

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