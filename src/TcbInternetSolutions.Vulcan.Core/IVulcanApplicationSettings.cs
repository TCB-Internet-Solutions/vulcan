namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Settings for Vulcan connections to Elastic Search
    /// </summary>
    public interface IVulcanApplicationSettings
    {
        /// <summary>
        /// Enables Http Compression
        /// </summary>
        bool EnableHttpCompression { get; }

        /// <summary>
        /// Vulcan Index prefix name
        /// </summary>
        string IndexNamePrefix { get; }

        /// <summary>
        /// Is debugging enabled
        /// </summary>
        bool IsDebugMode { get; }

        /// <summary>
        /// Vulcan password form connecting to Elasticsearch
        /// </summary>
        string Password { get; }

        /// <summary>
        /// URL to Elasticsearch instance
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Username to connect to Elasticsearch
        /// </summary>
        string Username { get; }
    }
}