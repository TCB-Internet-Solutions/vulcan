using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Configuration;
using System.Web.Configuration;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default Vulcan Client Connection settings, using a single connection pool and app setting values
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanClientConnectionSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanClientConnectionSettings : IVulcanClientConnectionSettings
    {
        IVulcanConnectionPoolFactory _VulcanConnectionpoolFactory;

        /// <summary>
        /// Injected constructor
        /// </summary>
        /// <param name="connectionpoolFactory"></param>
        public VulcanClientConnectionSettings(IVulcanConnectionPoolFactory connectionpoolFactory)
        {
            _VulcanConnectionpoolFactory = connectionpoolFactory;
        }

        /// <summary>
        /// Gets common settings from AppSetting keys 'VulcanUrl', 'VulcanIndex', 'VulcanUsername' (optional), 'VulcanPassword' (optional), 'VulcanEnableHttpCompression' (optional true/false)
        /// </summary>
        public virtual ConnectionSettings ConnectionSettings => CommonSettings();

        /// <summary>
        /// Value of AppSetting 'VulcanIndex'
        /// </summary>
        public virtual string Index => ConfigurationManager.AppSettings["VulcanIndex"];

        /// <summary>
        /// Common connection settings
        /// </summary>
        /// <returns></returns>
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

            var connectionPool = _VulcanConnectionpoolFactory.CreateConnectionPool(url);
            var settings = new ConnectionSettings(connectionPool, s => new VulcanCustomJsonSerializer(s));
            var username = ConfigurationManager.AppSettings["VulcanUsername"];
            var password = ConfigurationManager.AppSettings["VulcanPassword"];

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                settings.BasicAuthentication(username, password);
            }

            bool enableCompression = false;
            bool.TryParse(ConfigurationManager.AppSettings["VulcanEnableHttpCompression"], out enableCompression);

            // Enable bytes to be retrieved in debug mode
            settings.DisableDirectStreaming(isDebugMode);

            // Enable compression
            settings.EnableHttpCompression(enableCompression);

            return settings;
        }
    }
}
