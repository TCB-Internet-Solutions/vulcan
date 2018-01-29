using Elasticsearch.Net;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
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
        private readonly IVulcanConnectionPoolFactory _vulcanConnectionpoolFactory;
        private readonly IEnumerable<IVulcanIndexingModifier> _vulcanIndexerModifers;        
        private readonly IVulcanModfiySerializerSettings _vulcanModfiySerializerSettings;

        /// <summary>
        /// Injected constructor
        /// </summary>
        /// <param name="connectionpoolFactory"></param>
        /// <param name="vulcanIndexingModifiers"></param>
        /// <param name="vulcanModfiySerializerSettings"></param>
        public VulcanClientConnectionSettings
        (
            IVulcanConnectionPoolFactory connectionpoolFactory,
            IEnumerable<IVulcanIndexingModifier> vulcanIndexingModifiers,            
            IVulcanModfiySerializerSettings vulcanModfiySerializerSettings)
        {
            _vulcanConnectionpoolFactory = connectionpoolFactory;
            _vulcanIndexerModifers = vulcanIndexingModifiers;            
            _vulcanModfiySerializerSettings = vulcanModfiySerializerSettings;
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
            var section = ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
            var isDebugMode = section?.Debug ?? false;
            var url = ConfigurationManager.AppSettings["VulcanUrl"];
            var index = ConfigurationManager.AppSettings["VulcanIndex"];

            if (string.IsNullOrWhiteSpace(url) || url == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Url in AppSettings");
            }

            if (string.IsNullOrWhiteSpace(index) || index == "SET THIS")
            {
                throw new Exception("You need to specify the Vulcan Index in AppSettings");
            }

            var connectionPool = _vulcanConnectionpoolFactory.CreateConnectionPool(url);            
            var settings = new ConnectionSettings(connectionPool, CreateJsonSerializer);
            var username = ConfigurationManager.AppSettings["VulcanUsername"];
            var password = ConfigurationManager.AppSettings["VulcanPassword"];

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                settings.BasicAuthentication(username, password);
            }

            bool.TryParse(ConfigurationManager.AppSettings["VulcanEnableHttpCompression"], out var enableCompression);

            // Enable bytes to be retrieved in debug mode
            settings.DisableDirectStreaming(isDebugMode);

            // Enable compression
            settings.EnableHttpCompression(enableCompression);

            return settings;
        }

        /// <summary>
        /// Creates default serializer for Vulcan, only override in advanced cases
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected virtual IElasticsearchSerializer CreateJsonSerializer(ConnectionSettings s)
        {
            return new VulcanCustomJsonSerializer(s, _vulcanIndexerModifers, _vulcanModfiySerializerSettings.Modifier);
        }
    }
}