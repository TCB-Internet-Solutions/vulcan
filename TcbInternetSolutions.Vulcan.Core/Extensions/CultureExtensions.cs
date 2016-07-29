using System.Globalization;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    public static class CultureExtensions
    {
        public static string GetCultureName(this CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture) ? "invariant" : culture.Name;
    }
}
