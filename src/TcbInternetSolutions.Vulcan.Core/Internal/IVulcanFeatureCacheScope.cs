using System;

namespace TcbInternetSolutions.Vulcan.Core.Internal
{
    /// <summary>
    /// Feature flag for testing cache scoping
    /// </summary>
    public interface IVulcanFeatureCacheScope : IVulcanFeature
    {
        /// <summary>
        /// Scope cache duration
        /// </summary>
        TimeSpan CacheDuration { get; }
    }
}
