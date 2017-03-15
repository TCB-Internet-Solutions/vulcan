using EPiServer.Web;
using System.Collections.Generic;
using System;
using EPiServer;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default CMS content indexer
    /// </summary>
    public class VulcanCmsIndexer : IVulcanContentIndexer
    {
        public int ClearCacheItemInterval => 100;

        /// <summary>
        /// Indexer Name
        /// </summary>
        public string IndexerName => "CMS Content";

        public void ClearCache()
        {
            CacheManager.Clear();
        }

        /// <summary>
        /// Indexer root
        /// </summary>
        /// <returns></returns>
        public virtual KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");
    }
}
