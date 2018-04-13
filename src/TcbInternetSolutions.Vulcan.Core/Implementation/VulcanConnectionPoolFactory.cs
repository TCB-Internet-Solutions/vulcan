using System;
using System.Linq;
using Elasticsearch.Net;
using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Creates single and static node connection pools
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanConnectionPoolFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanConnectionPoolFactory : IVulcanConnectionPoolFactory
    {
        /// <summary>
        /// Creates single and static node connection pools from given url.
        /// </summary>
        public IConnectionPool CreateConnectionPool(string vulcanUrl)
        {
            if (string.IsNullOrWhiteSpace(vulcanUrl)) throw new ArgumentNullException(nameof(vulcanUrl));

            IConnectionPool connectionPool;
            var urls = vulcanUrl.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (urls.Length == 1)
            {
                connectionPool = new SingleNodeConnectionPool(new Uri(vulcanUrl));
            }
            else
            {
                var nodeUris = urls.Select(u => new Uri(u));
                connectionPool = new StaticConnectionPool(nodeUris);
            }

            return connectionPool;
        }
    }
}