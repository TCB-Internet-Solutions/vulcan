using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public static class VulcanFieldHelper
    {
        public static string GetPriceField(string marketId = null, string currencyCode = null)
        {
            return GetPriceField("prices", marketId, currencyCode);
        }

        public static string GetPriceLowField(string marketId = null, string currencyCode = null)
        {
            return GetPriceField("pricesLow", marketId, currencyCode);
        }

        public static string GetPriceHighField(string marketId = null, string currencyCode = null)
        {
            return GetPriceField("pricesHigh", marketId, currencyCode);
        }

        private static string GetPriceField(string propertyName, string marketId, string currencyCode)
        {
            if (marketId == null) marketId = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().MarketId.Value;
            if (currencyCode == null) currencyCode = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().DefaultCurrency.CurrencyCode;

            return "__" + propertyName + "." + marketId + "_" + currencyCode;
        }
    }
}
