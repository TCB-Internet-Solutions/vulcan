using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Markets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public class VulcanCommerceIndexingModifier : IVulcanIndexingModifier
    {
        public void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            if (content is VariationContent)
            {
                var variation = content as VariationContent;

                var streamWriter = new StreamWriter(writableStream);

                streamWriter.Write(",\"__prices\":{");

                var first = true;

                foreach (var market in ServiceLocator.Current.GetInstance<IMarketService>().GetAllMarkets())
                {
                    if(variation.IsAvailableInMarket(market.MarketId)) // no point adding price if not available in that market
                    {
                        var prices = new Dictionary<string, decimal>();

                        var variantPrices = variation.GetPrices(market.MarketId, Mediachase.Commerce.Pricing.CustomerPricing.AllCustomers);

                        if (variantPrices != null)
                        {
                            foreach (var price in variantPrices)
                            {
                                if (price.MinQuantity == 0 && price.CustomerPricing == Mediachase.Commerce.Pricing.CustomerPricing.AllCustomers) // this is a default price
                                {
                                    if(!prices.ContainsKey(price.UnitPrice.Currency.CurrencyCode))
                                    {
                                        prices.Add(price.UnitPrice.Currency.CurrencyCode, 0);
                                    }

                                    prices[price.UnitPrice.Currency.CurrencyCode] = price.UnitPrice.Amount;
                                }
                            }
                        }

                        if(prices.Count > 0)
                        {
                            if(first)
                            {
                                first = false;
                            }
                            else
                            {
                                streamWriter.Write(",");
                            }

                            var firstPrice = true;

                            foreach(var price in prices)
                            {
                                if(firstPrice)
                                {
                                    firstPrice = false;
                                }
                                else
                                {
                                    streamWriter.Write(",");
                                }

                                streamWriter.Write("\"" + market.MarketId.Value + "_" + price.Key + "\":" + price.Value.ToString());
                            }
                        }
                    }
                }

                streamWriter.Write("}");

                streamWriter.Flush();
            }
        }
    }
}
