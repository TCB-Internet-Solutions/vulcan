using Nest;
using Newtonsoft.Json;
using System;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Provides access to modify serialization settings
    /// </summary>
    public interface IVulcanModfiySerializerSettings
    {
        /// <summary>
        /// Modifier for serializer settings
        /// </summary>
        void Modifier(JsonSerializerSettings jsonSerializerSettings, IConnectionSettingsValues connectionSettingsValues);
    }
}
