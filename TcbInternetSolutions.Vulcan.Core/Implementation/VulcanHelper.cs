using Elasticsearch.Net;
using EPiServer.Core;
using EPiServer.DataAbstraction.RuntimeModel;
using Nest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    internal static class VulcanHelper
    {
        public static VulcanClient GetClient()
        {
            var connectionPool = new SingleNodeConnectionPool(new Uri(ConfigurationManager.AppSettings["VulcanUrl"]));
            var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));
            settings.InferMappingFor<ContentData>(pd => pd.Ignore(p => p.Property));
            settings.InferMappingFor<ContentMixin>(pd => pd.Ignore(p => p.MixinInstance));
            settings.InferMappingFor<PageData>(pd => pd.Ignore(p => p.PageName));
            settings.DefaultIndex(Index);

            return new VulcanClient(settings);
        }

        public static string Index
        {
            get
            {
                return ConfigurationManager.AppSettings["VulcanIndex"];
            }
        }

        public static void DeleteIndex()
        {
            var client = GetClient();

            client.DeleteIndex(Index);
        }

        public static KeyValuePair<ContentReference, string> GetLocalizedReference(string Id)
        {
            var split = Id.Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);

            return new KeyValuePair<ContentReference, string>(new ContentReference(split[0]), split.Length == 1 ? null : split[1]);
        }
    }
}
