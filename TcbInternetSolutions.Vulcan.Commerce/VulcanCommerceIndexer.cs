using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Caching;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexer : IVulcanContentIndexer
    {
        private readonly ReferenceConverter _ReferenceConverter;

        public VulcanCommerceIndexer(ReferenceConverter referenceConverter)
        {
            _ReferenceConverter = referenceConverter;
        }

        public int ClearCacheItemInterval => 100;

        public string IndexerName => "Commerce Content";

        public void ClearCache()
        {
            CacheHelper.Clear(string.Empty);
        }

        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(_ReferenceConverter.GetRootLink(), "Commerce");
    }
}
