using Elasticsearch.Net;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Configuration;
using System.Web;
using System.Web.Configuration;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ServiceConfiguration(typeof(IVulcanClientConnectionSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanClientConnectionSettings : IVulcanClientConnectionSettings
    { 
        public virtual ConnectionSettings ConnectionSettings => CommonSettings();

        public virtual string Index => ConfigurationManager.AppSettings["VulcanIndex"];

        protected virtual ConnectionSettings CommonSettings()
        {
            CompilationSection section = ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
            bool isDebugMode = section != null ? section.Debug : false;
            var url = ConfigurationManager.AppSettings["VulcanUrl"];
            var Index = ConfigurationManager.AppSettings["VulcanIndex"];

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(url) || url == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Url in AppSettings");
            }

            if (string.IsNullOrWhiteSpace(Index) || string.IsNullOrWhiteSpace(Index) || Index == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Index in AppSettings");
            }

            var connectionPool = new SingleNodeConnectionPool(new Uri(url));
            var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));
            var username = ConfigurationManager.AppSettings["VulcanUsername"];
            var password = ConfigurationManager.AppSettings["VulcanPassword"];

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                settings.BasicAuthentication(username, password);
            }

            // Enable bytes to be retrieved in debug mode
            settings.DisableDirectStreaming(isDebugMode);

            return settings;
        }
    }
}
