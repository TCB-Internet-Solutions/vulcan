using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Extensions;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class VulcanPriceReindexTrigger : IInitializableModule
    {
        private IVulcanHandler _vulcanHandler;
        private ReferenceConverter _referenceConverter;
        private IContentLoader _contentLoader;

        public void Initialize(InitializationEngine context)
        {
            var broadcaster = context.Locate.Advanced.GetInstance<CatalogKeyEventBroadcaster>();
            _contentLoader = context.Locate.ContentLoader();
            _referenceConverter = context.Locate.ReferenceConverter();
            _vulcanHandler = context.Locate.Advanced.GetInstance<IVulcanHandler>();

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
            var clients = _vulcanHandler.GetClients();
            if (clients?.Any() != true) return;

            foreach (var client in clients)
            {
                foreach (var variantCode in variantCodes)
                {
                    var variantLink = _referenceConverter.GetContentLink(variantCode);

                    if (ContentReference.IsNullOrEmpty(variantLink)) continue;

                    var variant = _contentLoader.Get<IContent>(variantLink, client.Language);

                    if (variant == null) continue;

                    var existing = client.SearchContent<VariationContent>(s => s.Query(q => q.Term(v => v.Code, variantCode)));

                    if (existing.Total > 0)
                    {
                        client.IndexContent(variant);
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