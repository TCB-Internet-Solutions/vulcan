using EPiServer.Core;
using Mediachase.Commerce;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Commerce.Extensions
{
    public static class PriceExtensions
    {
        public static decimal GetPrice(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice(((VulcanContentHit) contentHit).__prices, marketId, currencyCode) : 0;

        public static decimal GetPriceLow(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice(((VulcanContentHit) contentHit).__pricesLow, marketId, currencyCode) : 0;

        public static decimal GetPriceHigh(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice(((VulcanContentHit) contentHit).__pricesHigh, marketId, currencyCode) : 0;

        private static decimal GetPrice(IReadOnlyDictionary<string, decimal> priceDictionary, string marketId, string currencyCode, ICurrentMarket currentMarket = null)
        {
            currentMarket = currentMarket ?? VulcanHelper.GetService<ICurrentMarket>();
            if (marketId == null) marketId = currentMarket.GetCurrentMarket().MarketId.Value;
            if (currencyCode == null) currencyCode = currentMarket.GetCurrentMarket().DefaultCurrency.CurrencyCode;

            var key = marketId + "_" + currencyCode;

            if(priceDictionary != null && priceDictionary.ContainsKey(key))
            {
                return priceDictionary[key];
            }

            return 0;
        }
    }
}
