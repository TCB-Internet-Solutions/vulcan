namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Vulcan string extesions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Escapes given string as valid JSON
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string JsonEscapeString(this string s)
        {
            return Newtonsoft.Json.JsonConvert.ToString(s);
        }
    }
}