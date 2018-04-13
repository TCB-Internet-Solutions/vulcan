using EPiServer.ServiceLocation;

#if NET461
using System.Configuration;
using System.Web.Configuration;
#endif

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default implementation of settings
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanApplicationSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanApplicationSettings : IVulcanApplicationSettings
    {
#if NET461
        /// <summary>
        /// Netframework constructor
        /// </summary>
        public VulcanApplicationSettings()
        {
            var section = ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
            IsDebugMode = section?.Debug ?? false;
            Url = ConfigurationManager.AppSettings["Url"];
            IndexNamePrefix = ConfigurationManager.AppSettings["IndexNamePrefix"];
            Username = ConfigurationManager.AppSettings["Username"];
            Password = ConfigurationManager.AppSettings["Password"];
            bool.TryParse(ConfigurationManager.AppSettings["VulcanEnableHttpCompression"], out var enableCompression);
            EnableHttpCompression = enableCompression;
        }
#else
        /// <summary>
        /// Netstandard constructor
        /// </summary>
        public VulcanApplicationSettings()
        {
            // todo: add settings for netcore
            IsDebugMode = false;
            EnableHttpCompression = false;
            Url = string.Empty;
            IndexNamePrefix = string.Empty;
            Password = null;
            Username = null;
        }
#endif

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