namespace TcbInternetSolutions.Vulcan.Core
{
    using Nest;
    using System;

    /// <summary>
    /// Used to customize mappings and analyzers for Vulcan clients, any class that inherits must have an empty public constructor!
    /// </summary>
    public interface IVulcanCustomizer
    {
        /// <summary>
        /// Used to add custom mappings
        /// </summary>
        Func<IVulcanClient, IPutMappingResponse> CustomMapper { get; }

        /// <summary>
        /// Used to create custom analyzers such as EdgeNGram for autocomplete.
        /// </summary>
        Func<IVulcanClient, IUpdateIndexSettingsResponse> CustomIndexUpdater { get; }
    }
}
