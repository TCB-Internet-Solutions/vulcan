namespace TcbInternetSolutions.Vulcan.Core.Internal
{
    /// <summary>
    /// Feature flag to preview new functionality
    /// </summary>
    public interface IVulcanFeature
    {
        /// <summary>
        /// Determines if feature is enabled, mainly used around previewing new functionality
        /// </summary>
        bool Enabled { get; }
    }
}