namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Settings for the Indexing scheduled job
    /// </summary>
    public interface IVulcanIndexContentJobSettings
    {
        /// <summary>
        /// Parallel looping on indexers
        /// </summary>
        bool EnableParallelIndexers { get; }

        /// <summary>
        /// Parallel looping on content
        /// </summary>
        bool EnableParallelContent { get; }

        /// <summary>
        /// Always up on indexing
        /// </summary>
        bool EnableAlwaysUp { get; }
    }
}