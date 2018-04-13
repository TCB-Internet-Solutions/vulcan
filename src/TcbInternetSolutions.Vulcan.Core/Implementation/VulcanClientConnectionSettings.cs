using Elasticsearch.Net;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;

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
        private readonly VulcanApplicationSettings _vulcanApplicationSettings;

        /// <summary>
        /// Injected constructor
        /// </summary>
        /// <param name="connectionpoolFactory"></param>
        /// <param name="vulcanIndexingModifiers"></param>
        /// <param name="vulcanModfiySerializerSettings"></param>
        /// <param name="vulcanApplicationSettings"></param>
        public VulcanClientConnectionSettings
        (
            IVulcanConnectionPoolFactory connectionpoolFactory,
            IEnumerable<IVulcanIndexingModifier> vulcanIndexingModifiers,            
            IVulcanModfiySerializerSettings vulcanModfiySerializerSettings,
            VulcanApplicationSettings vulcanApplicationSettings
            )
        {
            _vulcanConnectionpoolFactory = connectionpoolFactory;
            _vulcanIndexerModifers = vulcanIndexingModifiers;            
            _vulcanModfiySerializerSettings = vulcanModfiySerializerSettings;
            _vulcanApplicationSettings = vulcanApplicationSettings;
        }

        /// <summary>
        /// Gets common settings from AppSetting keys 'Url', 'IndexNamePrefix', 'Username' (optional), 'Password' (optional), 'VulcanEnableHttpCompression' (optional true/false)
        /// </summary>
        public virtual ConnectionSettings ConnectionSettings => CommonSettings();

        /// <summary>
        /// Value of AppSetting 'IndexNamePrefix'
        /// </summary>
        public virtual string Index => _vulcanApplicationSettings.IndexNamePrefix;

        /// <summary>
        /// Common connection settings
        /// </summary>
        /// <returns></returns>
        protected virtual ConnectionSettings CommonSettings()
        {
            var url = _vulcanApplicationSettings.Url;
            var index = _vulcanApplicationSettings.IndexNamePrefix;

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
            var username = _vulcanApplicationSettings.Username;
            var password = _vulcanApplicationSettings.Password;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                settings.BasicAuthentication(username, password);
            }

            // Enable bytes to be retrieved in debug mode
            settings.DisableDirectStreaming(_vulcanApplicationSettings.IsDebugMode);

            // Enable compression
            settings.EnableHttpCompression(_vulcanApplicationSettings.EnableHttpCompression);

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