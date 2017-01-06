using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Elasticsearch.Net;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanConnectionPoolFactory
    {
        public static IConnectionPool CreateConnectionPool(string vulcanUrl)
        {
            if (string.IsNullOrEmpty(vulcanUrl)) throw new ArgumentNullException(nameof(vulcanUrl));

            IConnectionPool connectionPool;
            if (vulcanUrl.Contains(";"))
            {
                var urls = Regex.Split(vulcanUrl, ";");

                var nodeUris = urls.Select(u => new Uri(u));
                connectionPool = new StaticConnectionPool(nodeUris);
            }
            else
            {
                connectionPool = new SingleNodeConnectionPool(new Uri(vulcanUrl));
            }

            return connectionPool;
        }
    }
}