using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Caching;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core.Internal;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexer : IVulcanContentIndexerWithCacheClearing
    {
        private readonly ReferenceConverter _referenceConverter;

        public VulcanCommerceIndexer(ReferenceConverter referenceConverter)
        {
            _referenceConverter = referenceConverter;
        }

        public int ClearCacheItemInterval => 100;

        public string IndexerName => "Commerce Content";

        public void ClearCache()
        {
            CacheHelper.Clear(string.Empty);
        }

        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(_referenceConverter.GetRootLink(), "Commerce");
    }
}
