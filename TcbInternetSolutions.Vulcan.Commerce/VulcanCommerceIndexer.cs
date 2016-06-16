using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexer : IVulcanIndexer
    {
        public Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot()
        {
            return new KeyValuePair<EPiServer.Core.ContentReference, string>(ReferenceConverter.Service.GetRootLink(), "Commerce");
        }
    }
}
