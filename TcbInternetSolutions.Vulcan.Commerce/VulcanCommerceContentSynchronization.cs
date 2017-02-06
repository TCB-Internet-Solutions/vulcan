using EPiServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Events;
using EPiServer.Commerce.Catalog.Provider;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]

    public class VulcanCommerceContentSynchronization : IInitializableModule
    {
        public Injected<CatalogEventHandler> CatalogEventHandler { get; set; }

        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public void Initialize(InitializationEngine context)
        {
            CatalogEventHandler.Service.RelationsUpdated += VulcanCommerceContentSynchronization_RelationsUpdated;
        }

        private void VulcanCommerceContentSynchronization_RelationsUpdated(object sender, EPiServer.ContentEventArgs e)
        {
            VulcanHandler.Service.IndexContentEveryLanguage(e.ContentLink);
        }

        public void Uninitialize(InitializationEngine context)
        {
            CatalogEventHandler.Service.RelationsUpdated -= VulcanCommerceContentSynchronization_RelationsUpdated;
        }
    }
}
