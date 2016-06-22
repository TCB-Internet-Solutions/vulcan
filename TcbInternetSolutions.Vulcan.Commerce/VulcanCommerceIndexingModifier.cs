using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Markets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexingModifier : IVulcanIndexingModifier
    {
        public void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            if (content is VariationContent)
            {
                var streamWriter = new StreamWriter(writableStream);

                streamWriter.Write(",\"__prices\":{");

                WritePrices(streamWriter, GetDefaultPrices(content as VariationContent));

                streamWriter.Write("}");

                streamWriter.Flush();
            }
            else if(content is ProductContent)
            {
                var pricesLow = new Dictionary<string, Dictionary<string, decimal>>();
                var pricesHigh = new Dictionary<string, Dictionary<string, decimal>>();

                var variants = ServiceLocator.Current.GetInstance<IContentLoader>().GetItems((content as ProductContent).GetVariants(), (content as ProductContent).Language);

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

                var streamWriter = new StreamWriter(writableStream);

                streamWriter.Write(",\"__pricesLow\":{");

                WritePrices(streamWriter, pricesLow);

                streamWriter.Write("}");

                streamWriter.Write(",\"__pricesHigh\":{");

                WritePrices(streamWriter, pricesHigh);

                streamWriter.Write("}");

                streamWriter.Flush();
            }
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

                            streamWriter.Write("\"" + market.Key + "_" + price.Key + "\":" + price.Value.ToString());
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
    }
}
