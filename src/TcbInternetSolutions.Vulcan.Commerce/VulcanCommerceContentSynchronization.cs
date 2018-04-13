using EPiServer.Commerce.Catalog.Provider;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
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
