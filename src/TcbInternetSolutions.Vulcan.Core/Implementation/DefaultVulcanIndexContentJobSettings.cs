using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default Index job settings
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanIndexContentJobSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultVulcanIndexContentJobSettings : IVulcanIndexContentJobSettings
    {
        bool IVulcanIndexContentJobSettings.EnableParallelIndexers => false;

        bool IVulcanIndexContentJobSettings.EnableParallelContent => false;

        bool IVulcanIndexContentJobSettings.EnableAlwaysUp => false;
    }
}