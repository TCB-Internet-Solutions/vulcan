using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;
using TcbInternetSolutions.Vulcan.Core;
using EPiServer.Commerce.Catalog.ContentTypes;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class VulcanPriceReindexTrigger : IInitializableModule
    {
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public void Initialize(InitializationEngine context)
        {
            var broadcaster = context.Locate.Advanced.GetInstance<CatalogKeyEventBroadcaster>();

            broadcaster.InventoryUpdated += Broadcaster_InventoryUpdated;

            broadcaster.PriceUpdated += Broadcaster_PriceUpdated;
        }

        private void Broadcaster_PriceUpdated(object sender, PriceUpdateEventArgs e)
        {
            ReindexVariations(e.CatalogKeys.Select(ck => ck.CatalogEntryCode));
        }

        private void Broadcaster_InventoryUpdated(object sender, InventoryUpdateEventArgs e)
        {
            ReindexVariations(e.CatalogKeys.Select(ck => ck.CatalogEntryCode));
        }

        private void ReindexVariations(IEnumerable<string> variantCodes)
        {
            var clients = VulcanHandler.Service.GetClients();

            if (clients != null && clients.Any())
            {
                foreach (var client in clients)
                {
                    foreach (var variantCode in variantCodes)
                    {
                        var variantLink = ServiceLocator.Current.GetInstance<ReferenceConverter>().GetContentLink(variantCode);

                        if (!ContentReference.IsNullOrEmpty(variantLink))
                        {
                            var variant = ServiceLocator.Current.GetInstance<IContentLoader>().Get<IContent>(variantLink, client.Language);

                            if (variant != null)
                            {
                                var existing = client.SearchContent<VariationContent>(s => s.Query(q => q.Term(v => v.Code, variantCode)));

                                if (existing.Total > 0)
                                {
                                    client.IndexContent(variant);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            var broadcaster = context.Locate.Advanced.GetInstance<CatalogKeyEventBroadcaster>();

            broadcaster.InventoryUpdated -= Broadcaster_InventoryUpdated;

            broadcaster.PriceUpdated -= Broadcaster_PriceUpdated;
        }
    }
}