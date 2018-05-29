using Mediachase.Commerce;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public static class VulcanFieldHelper
    {
        public static string GetPriceField(string marketId = null, string currencyCode = null) => GetPriceField("prices", marketId, currencyCode);

        public static string GetPriceLowField(string marketId = null, string currencyCode = null) => GetPriceField("pricesLow", marketId, currencyCode);

        public static string GetPriceHighField(string marketId = null, string currencyCode = null) => GetPriceField("pricesHigh", marketId, currencyCode);

        private static string GetPriceField(string propertyName, string marketId, string currencyCode, ICurrentMarket currentMarket = null)
        {
            currentMarket = currentMarket ?? VulcanHelper.GetService<ICurrentMarket>();
            if (marketId == null) marketId = currentMarket.GetCurrentMarket().MarketId.Value;
            if (currencyCode == null) currencyCode = currentMarket.GetCurrentMarket().DefaultCurrency.CurrencyCode;

            return "__" + propertyName + "." + marketId + "_" + currencyCode;
        }
    }
}
