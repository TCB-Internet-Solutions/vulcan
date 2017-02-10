using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Markets;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexingModifier : IVulcanIndexingModifier
    {
        public Injected<ILinksRepository> LinksRepository { get; set; }

        public void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);

            if (content is VariationContent)
            {
                streamWriter.Write(",\"__prices\":{");
                WritePrices(streamWriter, GetDefaultPrices(content as VariationContent));
                streamWriter.Write("}");
            }
            else if(content is ProductContent)
            {
                var pricesLow = new Dictionary<string, Dictionary<string, decimal>>();
                var pricesHigh = new Dictionary<string, Dictionary<string, decimal>>();
                var variants = ServiceLocator.Current.GetInstance<IContentLoader>()
                    .GetItems((content as ProductContent).GetVariants(), (content as ProductContent).Language);

                if (variants != null)
                {
                    foreach (VariationContent variant in variants)
                    {
                        var markets = GetDefaultPrices(variant);

                        if(markets != null)
                        {
                            foreach(var market in markets)
                            {
                                if(!pricesLow.ContainsKey(market.Key))
                                {
                                    pricesLow.Add(market.Key, new Dictionary<string, decimal>());
                                }

                                if (!pricesHigh.ContainsKey(market.Key))
                                {
                                    pricesHigh.Add(market.Key, new Dictionary<string, decimal>());
                                }

                                if(market.Value.Any())
                                {
                                    foreach(var price in market.Value)
                                    {
                                        if(!pricesLow[market.Key].ContainsKey(price.Key))
                                        {
                                            pricesLow[market.Key].Add(price.Key, price.Value);
                                        }
                                        else
                                        {
                                            if(price.Value < pricesLow[market.Key][price.Key])
                                            {
                                                pricesLow[market.Key][price.Key] = price.Value;
                                            }
                                        }

                                        if (!pricesHigh[market.Key].ContainsKey(price.Key))
                                        {
                                            pricesHigh[market.Key].Add(price.Key, price.Value);
                                        }
                                        else
                                        {
                                            if (price.Value > pricesHigh[market.Key][price.Key])
                                            {
                                                pricesHigh[market.Key][price.Key] = price.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                streamWriter.Write(",\"__pricesLow\":{");
                WritePrices(streamWriter, pricesLow);
                streamWriter.Write("}");
                streamWriter.Write(",\"__pricesHigh\":{");
                WritePrices(streamWriter, pricesHigh);
                streamWriter.Write("}");
            }

            // read permission compatibility for commerce content, since markets handle access
            var commercePermissionEntries = new AccessControlEntry[]
            {
                    new AccessControlEntry(EveryoneRole.RoleName, AccessLevel.Read),
                    new AccessControlEntry(AnonymousRole.RoleName, AccessLevel.Read)
            };

            streamWriter.Write(",\"" + VulcanFieldConstants.ReadPermission + "\":[");
            streamWriter.Write(string.Join(",", commercePermissionEntries.Select(x => "\"" + x.Name + "\"")));
            streamWriter.Write("]");

            streamWriter.Flush();
        }

        private void WritePrices(StreamWriter streamWriter, Dictionary<string, Dictionary<string, decimal>> markets)
        {
            if (markets != null)
            {
                var first = true;

                foreach (var market in markets)
                {
                    if (market.Value.Any())
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            streamWriter.Write(",");
                        }

                        var firstPrice = true;

                        foreach (var price in market.Value)
                        {
                            if (firstPrice)
                            {
                                firstPrice = false;
                            }
                            else
                            {
                                streamWriter.Write(",");
                            }

                            streamWriter.Write("\"" + market.Key + "_" + price.Key + "\":" + price.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
                        }
                    }
                }
            }
        }

        private Dictionary<string, Dictionary<string, decimal>> GetDefaultPrices(VariationContent variation)
        {
            var prices = new Dictionary<string, Dictionary<string, decimal>>();

            foreach (var market in ServiceLocator.Current.GetInstance<IMarketService>().GetAllMarkets())
            {
                if (variation.IsAvailableInMarket(market.MarketId)) // no point adding price if not available in that market
                {
                    prices.Add(market.MarketId.Value, new Dictionary<string, decimal>());

                    var variantPrices = variation.GetPrices(market.MarketId, Mediachase.Commerce.Pricing.CustomerPricing.AllCustomers);

                    if (variantPrices != null)
                    {
                        foreach (var price in variantPrices)
                        {
                            if (price.MinQuantity == 0 && price.CustomerPricing == Mediachase.Commerce.Pricing.CustomerPricing.AllCustomers) // this is a default price
                            {
                                if (!prices.ContainsKey(price.UnitPrice.Currency.CurrencyCode))
                                {
                                    prices[market.MarketId.Value].Add(price.UnitPrice.Currency.CurrencyCode, 0);
                                }

                                prices[market.MarketId.Value][price.UnitPrice.Currency.CurrencyCode] = price.UnitPrice.Amount;
                            }
                        }
                    }
                }
            }

            return prices;
        }

        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            var ancestors = new List<ContentReference>();

            if (content is VariationContent)
            {
                var productAncestors = LinksRepository.Service.GetRelationsByTarget(content.ContentLink)?.OfType<ProductVariation>();

                if (productAncestors != null && productAncestors.Any())
                {
                    ancestors.AddRange(productAncestors.Select(pa => pa.Source));

                    ancestors.AddRange(productAncestors.SelectMany(pa => GetAncestorCategoriesIterative(pa.Source)));
                }
            }

            // for these purposes, we assume that products cannot exist inside other products
            // variant may also exist directly inside a category

            ancestors.AddRange(GetAncestorCategoriesIterative(content.ContentLink));

            return ancestors.Distinct();
        }

        private IEnumerable<ContentReference> GetAncestorCategoriesIterative(ContentReference contentLink)
        {
            var ancestors = new List<ContentReference>();

            var categories = LinksRepository.Service.GetRelationsBySource(contentLink)?.OfType<NodeRelation>();

            if(categories != null && categories.Any())
            {
                ancestors.AddRange(categories.Select(pa => pa.Target));

                ancestors.AddRange(categories.SelectMany(c => GetAncestorCategoriesIterative(c.Target)));
            }

            return ancestors;
        }
    }
}
