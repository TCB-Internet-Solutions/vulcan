using Elasticsearch.Net;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ServiceConfiguration(typeof(IVulcanHandler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanHandler : IVulcanHandler
    {
        private static ILogger Logger = LogManager.GetLogger();

        public Injected<IContentLoader> ContentLoader { get; set; }

        private Dictionary<CultureInfo, VulcanClient> clients = new Dictionary<CultureInfo, VulcanClient>();

        private object lockObject = new object();

        public IVulcanClient[] GetClients()
        {
            var url = ConfigurationManager.AppSettings["VulcanUrl"];

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(url) || url == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Url in AppSettings");
            }

            if (string.IsNullOrWhiteSpace(Index) || string.IsNullOrWhiteSpace(Index) || Index == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Index in AppSettings");
            }

            var clients = new List<IVulcanClient>();

            var connectionPool = new SingleNodeConnectionPool(new Uri(url));
            var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));

            var username = ConfigurationManager.AppSettings["VulcanUsername"];
            var password = ConfigurationManager.AppSettings["VulcanPassword"];
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                settings.BasicAuthentication(username, password);
            }

            var client = new ElasticClient(settings);

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
        /// Get a Vulcan client
        /// </summary>
        /// <param name="language">Pass in null for current culture, a specific culture or CultureInfo.InvariantCulture to get a client for non-language specific data</param>
        /// <returns>A Vulcan client</returns>
        public IVulcanClient GetClient(CultureInfo language = null)
        {
            var cultureInfo = language == null ? CultureInfo.CurrentUICulture : language;

            lock (lockObject)
            {
                if (clients.ContainsKey(cultureInfo)) return clients[cultureInfo];

                // we now know what our culture is (current culture or invariant), but we need to choose the language analyzer

                var languageAnalyzer = VulcanHelper.GetAnalyzer(cultureInfo);

                var connectionPool = new SingleNodeConnectionPool(new Uri(ConfigurationManager.AppSettings["VulcanUrl"]));
                var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));
                settings.InferMappingFor<ContentMixin>(pd => pd.Ignore(p => p.MixinInstance));
                settings.DefaultIndex(VulcanHelper.GetIndexName(Index, cultureInfo));

                var username = ConfigurationManager.AppSettings["VulcanUsername"];
                var password = ConfigurationManager.AppSettings["VulcanPassword"];
                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    settings.BasicAuthentication(username, password);
                }

                var client = new VulcanClient(Index, settings, cultureInfo);

                // first let's check our version

                var nodesInfo = client.NodesInfo();

                if (nodesInfo == null)
                {
                    throw new Exception("Could not get Nodes info to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                }
                else
                {
                    if (nodesInfo.Nodes == null)
                    {
                        throw new Exception("Could not find valid nodes to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                    }
                    else
                    {
                        if (nodesInfo.Nodes.Count == 0)
                        {
                            throw new Exception("Could not find any valid nodes to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                        }
                        else
                        {
                            var node = nodesInfo.Nodes.First(); // just use first

                            if(string.IsNullOrWhiteSpace(node.Value.Version)) // just use first
                            {
                                throw new Exception("Could not find a version on node to check Elasticsearch Version. Check that you are correctly connected to Elasticsearch?");
                            }
                            else
                            {
                                if(node.Value.Version.StartsWith("1."))
                                {
                                    throw new Exception("Sorry, Vulcan only works with Elasticsearch version 2.x or higher. The Elasticsearch node you are currently connected to is version " + node.Value.Version);
                                }
                            }
                        }
                    }
                }

                client.PutIndexTemplate("analyzer_disabling", ad => ad
                        .Template("*") //match on all created indices
                        .Mappings(mappings => mappings.Map("_default_", map => map.DynamicTemplates(
                            dyn => dyn.DynamicTemplate("analyzer_template", dt => dt
                                .Match("*") //matches all fields
                                .MatchMappingType("string") //that are a string
                                .Mapping(dynmap => dynmap.String(s => s.NotAnalyzed().IncludeInAll(false).Fields(f => f.String(ana => ana.Name("analyzed").IncludeInAll(false).Store(true)
                                    )))))))));

                if (!client.IndexExists(VulcanHelper.GetIndexName(Index, cultureInfo)).Exists)
                {
                    var response = client.CreateIndex(VulcanHelper.GetIndexName(Index, cultureInfo));

                    if (!response.IsValid)
                    {
                        Logger.Error("Could not create index " + VulcanHelper.GetIndexName(Index, cultureInfo) + ": " + response.DebugInformation);
                    }
                }

                client.Refresh(VulcanHelper.GetIndexName(Index, cultureInfo));

                var closeResponse = client.CloseIndex(VulcanHelper.GetIndexName(Index, cultureInfo));

                if (!closeResponse.IsValid)
                {
                    Logger.Error("Could not close index " + VulcanHelper.GetIndexName(Index, cultureInfo) + ": " + closeResponse.DebugInformation);
                }

                InitializeAnalyzer(client);

                client.OpenIndex(VulcanHelper.GetIndexName(Index, cultureInfo));

                clients.Add(cultureInfo, client);

                return client;
            }
        }

        public string Index
        {
            get
            {
                return ConfigurationManager.AppSettings["VulcanIndex"];
            }
        }

        public void DeleteIndex()
        {
            lock (lockObject)
            {
                var connectionPool = new SingleNodeConnectionPool(new Uri(ConfigurationManager.AppSettings["VulcanUrl"]));
                var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));

                var username = ConfigurationManager.AppSettings["VulcanUsername"];
                var password = ConfigurationManager.AppSettings["VulcanPassword"];
                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    settings.BasicAuthentication(username, password);
                }

                var client = new ElasticClient(settings); // use a raw elasticclient because we just need this to be quick

                var indices = client.CatIndices();

                if (indices != null && indices.Records != null && indices.Records.Any())
                {
                    foreach (var index in indices.Records.Where(i => i.Index.StartsWith(Index + "_")).Select(i => i.Index))
                    {
                        var response = client.DeleteIndex(index);

                        if (!response.IsValid)
                        {
                            Logger.Error("Could not run a delete index: " + response.DebugInformation);
                        }
                    }
                }

                clients = new Dictionary<CultureInfo, VulcanClient>(); // need to force a re-creation
            }
        }

        public void DeleteContentByLanguage(IContent content)
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

        public void DeleteContentEveryLanguage(IContent content)
        {
            if (!(content is ILocalizable))
            {
                var client = GetClient(CultureInfo.InvariantCulture);

                client.DeleteContent(content);
            }
            else
            {
                foreach (var language in (content as ILocalizable).ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.DeleteContent(content);
                }
            }
        }

        public void IndexContentByLanguage(IContent content)
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

        public void IndexContentEveryLanguage(IContent content)
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

                    client.IndexContent(ContentLoader.Service.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language));
                }
            }
        }

        private string[] GetSynonyms(VulcanClient client)
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

        private string GetStemmerLanguage(string language)
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

        private string GetStopwordsLanguage(string language)
        {
            switch (language)
            {
                case "cjk":
                    return "english";

                default:
                    return language;
            }
        }


        private string[] GetFilters(string language)
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

        private string[] GetElisionArticles(string language)
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

        private void InitializeAnalyzer(VulcanClient client)
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