using System;
using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Internal
{
    /// <summary>
    /// Default Cachescope preview functionality
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanFeature), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanFeatureCacheScope : IVulcanFeatureCacheScope
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanFeatureCacheScope()
        {
            Enabled = AppConfigurationHelper.TryGetBoolFromKey
            (
                key: nameof(VulcanFeatureCacheScope),
                defaultValue: false
            );
        }

        /// <summary>
        /// Default is disabled
        /// </summary>
        public virtual bool Enabled { get; }

        /// <summary>
        /// Default cache duration is 10 seconds
        /// </summary>
        public virtual TimeSpan CacheDuration { get; } = TimeSpan.FromSeconds(value: 10);
    }
}