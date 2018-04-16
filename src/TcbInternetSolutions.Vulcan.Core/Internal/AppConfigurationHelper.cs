#if NET461
using System.Configuration;
#endif

namespace TcbInternetSolutions.Vulcan.Core.Internal
{
    /// <summary>
    /// Netframework helper
    /// </summary>
    public static class AppConfigurationHelper
    {
        /// <summary>
        /// Netframework app key converter, if net core default value is returned
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool TryGetBoolFromKey(string key, bool defaultValue)
        {

            var keyValue = TryGetValueFromAppKey(key);
            if (string.IsNullOrWhiteSpace(keyValue)) return defaultValue;

            return bool.TryParse(keyValue, out var converted) ?
                converted :
                GetSetting(keyValue, defaultValue);
        }

        /// <summary>
        /// Netframework app key getter, netcore returns default value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string TryGetValueFromAppKey(string key, string defaultValue = null)
        {
#if NET461
            var readValue = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(readValue) ? defaultValue : readValue;
#else
            return defaultValue;

#endif
        }

        /// <summary>
        /// Netframework debug mode check, always false for netstandard
        /// </summary>
        /// <returns></returns>
        public static bool IsDebugMode()
        {
#if NET461
            var section = ConfigurationManager.GetSection("system.web/compilation") as System.Web.Configuration.CompilationSection;
            return section?.Debug ?? false;
#else
            return false;
#endif
        }

        // ReSharper disable once UnusedMember.Local
        private static bool GetSetting(string setting, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(setting))
            {
                return defaultValue;
            }

            return setting.Equals(value: "true") || setting.Equals(value: "1");
        }
    }
}
