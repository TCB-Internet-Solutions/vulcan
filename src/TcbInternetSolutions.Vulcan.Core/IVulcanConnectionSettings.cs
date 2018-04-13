using Nest;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Vulcan connection settings
    /// </summary>
    public interface IVulcanClientConnectionSettings
    {
        /// <summary>
        /// Index name
        /// </summary>
        string Index { get; }

        /// <summary>
        /// Connection settings
        /// </summary>
        ConnectionSettings ConnectionSettings { get; }
    }
}
