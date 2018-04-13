using EPiServer.ServiceLocation;
using Nest;
using Newtonsoft.Json;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Enables EscapeHtml for string escaping
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanModfiySerializerSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultVulcanModifySerializerSettings : IVulcanModfiySerializerSettings
    {        

        void IVulcanModfiySerializerSettings.Modifier(JsonSerializerSettings jsonSerializerSettings, IConnectionSettingsValues connectionSettingsValues)
        {
            jsonSerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
        }
    }
}
