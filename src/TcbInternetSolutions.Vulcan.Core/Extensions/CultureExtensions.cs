using System.Globalization;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Culture extensions
    /// </summary>
    public static class CultureExtensions
    {
        /// <summary>
        /// Gets name from cultureinfo
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string GetCultureName(this CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture) ? "invariant" : culture.Name;
    }
}
