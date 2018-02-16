using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    [ServiceConfiguration(typeof(IVulcanIndexingModifier), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCommerceIndexingModifier : IVulcanIndexingModifier
    {        
        private readonly IMarketService _marketService;
        private readonly IPriceDetailService _priceDetailService;
        private readonly IContentLoader _contentLoader;

        public VulcanCommerceIndexingModifier(IPriceDetailService priceDetailService, IMarketService marketService, IContentLoader contentLoader)
        {
            _priceDetailService = priceDetailService;
            _marketService = marketService;
            _contentLoader = contentLoader;
        }

        public void ProcessContent(IVulcanIndexingModifierArgs args)
        {
            switch (args.Content)
            {
                case VariationContent variationContent:

                    var marketPrices = GetDefaultPrices(variationContent);
                    var prices = new Dictionary<string, decimal>();

                    if (marketPrices?.Any() == true)
                    {
                        foreach (var market in marketPrices)
                        {
                            foreach (var price in market.Value)
                            {
                                prices[market.Key + "_" + price.Key] = price.Value;
                            }
                        }
                    }

                    if (prices.Any())
                    {
                        args.AdditionalItems["__prices"] = prices;
                    }
                    break;
                case ProductContent productContent:
                    var pricesLow = new Dictionary<string, Dictionary<string, decimal>>();
                    var pricesHigh = new Dictionary<string, Dictionary<string, decimal>>();
                    var variants = _contentLoader.GetItems(productContent.GetVariants(), productContent.Language);

                    if (variants != null)
                    {
                        foreach (var v in variants)
                        {
                            if (!(v is VariationContent variant)) continue;
                            var markets = GetDefaultPrices(variant);

                            if (markets == null) continue;

                            foreach (var market in markets)
                            {
                                if (!pricesLow.ContainsKey(market.Key))
                                {
                                    pricesLow.Add(market.Key, new Dictionary<string, decimal>());
                                }

                                if (!pricesHigh.ContainsKey(market.Key))
                                {
                                    pricesHigh.Add(market.Key, new Dictionary<string, decimal>());
                                }

                                if (!market.Value.Any()) continue;

                                foreach (var price in market.Value)
                                {
                                    if (!pricesLow[market.Key].ContainsKey(price.Key))
                                    {
                                        pricesLow[market.Key].Add(price.Key, price.Value);
                                    }
                                    else
                                    {
                                        if (price.Value < pricesLow[market.Key][price.Key])
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

                    var flatPricesLow = new Dictionary<string, decimal>();
                    var flatPricesHigh = new Dictionary<string, decimal>();

                    if (pricesLow.Any())
                    {
                        foreach (var market in pricesLow)
                        {
                            foreach (var price in market.Value)
                            {
                                flatPricesLow[market.Key + "_" + price.Key] = price.Value;
                            }
                        }
                    }
                    if (pricesHigh.Any())
                    {
                        foreach (var market in pricesHigh)
                        {
                            foreach (var price in market.Value)
                            {
                                flatPricesHigh[market.Key + "_" + price.Key] = price.Value;
                            }
                        }
                    }

                    if (flatPricesLow.Any()) args.AdditionalItems["__pricesLow"] = flatPricesLow;
                    if (flatPricesHigh.Any()) args.AdditionalItems["__pricesHigh"] = flatPricesHigh;

                    break;
            }

            // read permission compatibility for commerce content, since markets handle access
            var commercePermissionEntries = new[]
            {
                    new AccessControlEntry(EveryoneRole.RoleName, AccessLevel.Read),
                    new AccessControlEntry(AnonymousRole.RoleName, AccessLevel.Read)
            };

            args.AdditionalItems[VulcanFieldConstants.ReadPermission] = commercePermissionEntries.Select(x => x.Name);
            //streamWriter.Write(",\"" + VulcanFieldConstants.ReadPermission + "\":[");
            //streamWriter.Write(string.Join(",", commercePermissionEntries.Select(x => "\"" + x.Name + "\"")));
            //streamWriter.Write("]");

            //streamWriter.Flush();
        }

        private Dictionary<string, Dictionary<string, decimal>> GetDefaultPrices(VariationContent variation)
        {
            var prices = new Dictionary<string, Dictionary<string, decimal>>();

            foreach (var market in _marketService.GetAllMarkets())
            {
                if (!variation.IsAvailableInMarket(market.MarketId)) continue;

                prices.Add(market.MarketId.Value, new Dictionary<string, decimal>());

                var variantPrices = _priceDetailService.List(variation.ContentLink, market.MarketId,
                    new PriceFilter
                    {
                        Quantity = 0,
                        CustomerPricing = new[] { CustomerPricing.AllCustomers }
                    },
                    0,
                    9999,
                    out var totalCount
                ); // we are using 9,999 price values as the theoretical maximum

                if (variantPrices == null || totalCount <= 0) continue;

                foreach (var price in variantPrices)
                {                    
                    if (price.MinQuantity != 0 || price.CustomerPricing != CustomerPricing.AllCustomers) continue;// this is a default price

                    if (!prices.ContainsKey(price.UnitPrice.Currency.CurrencyCode))
                    {
                        prices[market.MarketId.Value].Add(price.UnitPrice.Currency.CurrencyCode, 0);
                    }

                    prices[market.MarketId.Value][price.UnitPrice.Currency.CurrencyCode] = price.UnitPrice.Amount;
                }
            }

            return prices;
        }
    }
}
