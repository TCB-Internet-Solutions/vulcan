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
            if (marketId == null) marketId = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().MarketId.Value;
            if (currencyCode == null) currencyCode = ServiceLocator.Current.GetInstance<ICurrentMarket>().GetCurrentMarket().DefaultCurrency.CurrencyCode;
            
            return "__prices." + marketId + "_" + currencyCode;
        }
    }
}
