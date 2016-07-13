using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexer : IVulcanContentIndexer
    {
        public string IndexerName => "Commerce Content";

        public Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(ReferenceConverter.Service.GetRootLink(), "Commerce");
    }
}
