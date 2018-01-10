﻿using EPiServer.Framework.Cache;
using EPiServer.Web;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default CMS content indexer
    /// </summary>
    public class VulcanCmsIndexer : IVulcanContentIndexer
    {
        private readonly ISynchronizedObjectInstanceCache _SynchronizedObjectInstanceCache;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="synchronizedObjectInstanceCache"></param>
        public VulcanCmsIndexer(ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            _SynchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
        }

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
            //CacheManager.Clear(); //this has been deprecated
            var cacheKeys = GetCacheKeys();

            foreach(var key in cacheKeys)
            {
                _SynchronizedObjectInstanceCache.RemoveLocal(key);
                _SynchronizedObjectInstanceCache.RemoveRemote(key);
            }
        }

        /// <summary>
        /// Indexer root
        /// </summary>
        /// <returns></returns>
        public virtual KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");

        private IEnumerable<string> GetCacheKeys()
        {
            var enumerator = System.Web.HttpRuntime.Cache.GetEnumerator();
            List<string> cacheKeys = new List<string>();

            while (enumerator.MoveNext())
            {
                cacheKeys.Add(enumerator.Key.ToString());
            }

            return cacheKeys;
        }
    }
}
