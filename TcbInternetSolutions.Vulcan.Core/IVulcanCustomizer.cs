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

        /// <summary>
        /// Used to create custom index templates for indexes. To override field mappings effectively set the order > 0. 
        /// <para>Also please note back-end UI searches required an 'analyzed' multi-field, so be mindful of custom property mapping.</para>
        /// </summary>
        Func<IVulcanClient, string, IPutIndexTemplateResponse> CustomIndexTemplate { get; }
    }
}
