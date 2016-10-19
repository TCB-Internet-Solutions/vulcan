namespace TcbInternetSolutions.Vulcan.Core
{
    using Nest;
    using System;

    public interface IVulcanCustomizer
    {
        Func<IVulcanClient, IPutMappingResponse> CustomMapper { get; }

        Func<IVulcanClient, IUpdateIndexSettingsResponse> CustomIndexUpdater { get; }
    }
}
