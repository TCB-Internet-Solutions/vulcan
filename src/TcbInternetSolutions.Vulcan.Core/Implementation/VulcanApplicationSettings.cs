using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default implementation of settings
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanApplicationSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanApplicationSettings : IVulcanApplicationSettings
    {
        /// <summary>
        /// Netframework constructor
        /// </summary>
        public VulcanApplicationSettings()
        {
            IsDebugMode = Internal.AppConfigurationHelper.IsDebugMode();
            Url = Internal.AppConfigurationHelper.TryGetValueFromAppKey(key: "VulcanUrl");
            IndexNamePrefix = Internal.AppConfigurationHelper.TryGetValueFromAppKey(key: "VulcanIndex");
            Username = Internal.AppConfigurationHelper.TryGetValueFromAppKey(key: "VulcanUsername");
            Password = Internal.AppConfigurationHelper.TryGetValueFromAppKey(key: "VulcanPassword");
            EnableHttpCompression = Internal.AppConfigurationHelper.TryGetBoolFromKey(key: "VulcanEnableHttpCompression", defaultValue: false);
        }

        /// <summary>
        /// Is debug mode enabled
        /// </summary>
        public virtual bool IsDebugMode { get; }

        /// <summary>
        /// Elastic Search URL
        /// </summary>
        public virtual string Url { get; }

        /// <summary>
        /// Index Name prefix, ex web.Env
        /// </summary>
        public virtual string IndexNamePrefix { get; }

        /// <summary>
        /// Username to Elasticsearch connection if needed
        /// </summary>
        public virtual string Username { get; }

        /// <summary>
        /// Password to Elasticsearch connection if needed
        /// </summary>
        public virtual string Password { get; }

        /// <summary>
        /// Is http compression enabled
        /// </summary>
        public virtual bool EnableHttpCompression { get; }
    }
}