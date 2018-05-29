using EPiServer.Framework.Cache;
using EPiServer.Web;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core.Internal;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default CMS content indexer
    /// </summary>
    public class VulcanCmsIndexer : IVulcanContentIndexerWithCacheClearing
    {
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="synchronizedObjectInstanceCache"></param>
        public VulcanCmsIndexer(ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
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
                _synchronizedObjectInstanceCache.RemoveLocal(key);
                _synchronizedObjectInstanceCache.RemoveRemote(key);
            }
        }

        /// <summary>
        /// Indexer root
        /// </summary>
        /// <returns></returns>
        public virtual KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");

        private static IEnumerable<string> GetCacheKeys()
        {
#if NET461
            var cacheKeys = new List<string>();
            var enumerator = System.Web.HttpRuntime.Cache.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var key = enumerator.Key?.ToString() ?? string.Empty;
                cacheKeys.Add(key);
            }

            return cacheKeys;
#else
            //todo: figure out netstandard alternative
            return Enumerable.Empty<string>();
#endif
        }
    }
}