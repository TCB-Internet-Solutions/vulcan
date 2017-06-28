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
        /// <summary>
        /// Default cache clear interval
        /// </summary>
        public int ClearCacheItemInterval => 100;

        /// <summary>
        /// Indexer Name
        /// </summary>
        public string IndexerName => "CMS Content";

        /// <summary>
        /// Default clears Episerver cache manager cache.
        /// </summary>
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
