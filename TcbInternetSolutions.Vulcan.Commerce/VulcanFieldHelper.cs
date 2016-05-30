using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    public static class VulcanFieldHelper
    {
        public static string GetPriceField(string marketId, string currencyCode)
        {
            return "__prices." + marketId + "_" + currencyCode;
        }
    }
}
