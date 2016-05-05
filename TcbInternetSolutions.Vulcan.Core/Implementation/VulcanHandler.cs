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
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ServiceConfiguration(typeof(IVulcanHandler),Lifecycle=ServiceInstanceScope.Singleton)]
    public class VulcanHandler : IVulcanHandler
    {
        private static ILogger Logger = LogManager.GetLogger();

        public Injected<IContentLoader> ContentLoader { get; set; }

        private Dictionary<CultureInfo, VulcanClient> clients = new Dictionary<CultureInfo, VulcanClient>();

        private object lockObject = new object();

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
                if(!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    settings.BasicAuthentication(username, password);
                }

                var client = new VulcanClient(Index, settings, cultureInfo);

                client.PutIndexTemplate("analyzer_disabling", ad => ad
                        .Template("*") //match on all created indices
                        .Mappings(mappings => mappings.Map("_default_", map => map.DynamicTemplates(
                            dyn => dyn.DynamicTemplate("analyzer_template", dt => dt
                                .Match("*") //matches all fields
                                .MatchMappingType("string") //that are a string
                                .Mapping(dynmap => dynmap.String(s => s.NotAnalyzed().IncludeInAll(false).Fields(f => f.String(ana => ana.Name("analyzed").Analyzer(languageAnalyzer).IncludeInAll(false).Store(true)
                                    )))))))));

                if (!client.IndexExists(VulcanHelper.GetIndexName(Index, cultureInfo)).Exists)
                {
                    var nestLanguage = VulcanHelper.GetLanguage(cultureInfo);

                    ICreateIndexResponse response;

                    if(nestLanguage == null || !nestLanguage.HasValue)
                    {
                        response = client.CreateIndex(VulcanHelper.GetIndexName(Index, cultureInfo));
                    }
                    else
                    {
                        response = client.CreateIndex(VulcanHelper.GetIndexName(Index, cultureInfo), i => i.Settings(s => s.Analysis(a => a.Analyzers(analyzers => analyzers.Language("default", sel => sel.Language(nestLanguage.Value)).Language("default_search", sel => sel.Language(nestLanguage.Value))))));
                    }

                    if(!response.IsValid)
                    {
                        Logger.Error("Could not create index " + VulcanHelper.GetIndexName(Index, cultureInfo) + ": " + response.DebugInformation);
                    }
                }

                clients.Add(cultureInfo, client);

                return client;
            }
        }

        private string Index
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

                clients = new Dictionary<CultureInfo,VulcanClient>(); // need to force a re-creation
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
            
            if(!(content is ILocalizable))
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
                foreach(var language in (content as ILocalizable).ExistingLanguages)
                {
                    var client = GetClient(language);

                    client.IndexContent(ContentLoader.Service.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language));
                }
            }
        }
    }
}
