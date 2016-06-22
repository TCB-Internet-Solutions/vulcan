using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Commerce.Extensions
{
    public static class PriceExtensions
    {
        public static decimal GetPrice(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice((contentHit as VulcanContentHit).__prices, marketId, currencyCode) : 0;

        public static decimal GetPriceLow(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice((contentHit as VulcanContentHit).__pricesLow, marketId, currencyCode) : 0;

        public static decimal GetPriceHigh(this IContent contentHit, string marketId = null, string currencyCode = null) =>
            contentHit is VulcanContentHit ? GetPrice((contentHit as VulcanContentHit).__pricesHigh, marketId, currencyCode) : 0;

        private static decimal GetPrice(Dictionary<string, decimal> priceDictionary, string marketId, string currencyCode)
        {
            if (marketId == null) marketId = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().MarketId.Value;
            if (currencyCode == null) currencyCode = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().DefaultCurrency.CurrencyCode;

            var key = marketId + "_" + currencyCode;

            if(priceDictionary != null && priceDictionary.ContainsKey(key))
            {
                return priceDictionary[key];
            }
            else
            {
                return 0;
            }
        }
    }
}
