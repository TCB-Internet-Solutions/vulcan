using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core;
using System;
using Mediachase.Commerce.Engine.Caching;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexer : IVulcanContentIndexer
    {
        public int ClearCacheItemInterval => 100;

        public string IndexerName => "Commerce Content";

        public Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public void ClearCache()
        {
            CacheHelper.Clear(string.Empty);
        }

        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(ReferenceConverter.Service.GetRootLink(), "Commerce");
    }
}
