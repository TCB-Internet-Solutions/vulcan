using Elasticsearch.Net;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Creates Elasticsearch connection pools
    /// </summary>
    public interface IVulcanConnectionPoolFactory
    {
        /// <summary>
        /// Create connection pool given url
        /// </summary>
        /// <param name="vulcanUrl"></param>
        /// <returns></returns>
        IConnectionPool CreateConnectionPool(string vulcanUrl);
    }
}
