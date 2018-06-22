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

        /// <summary>
        /// How much parallel allowed? Defaults to 4, will likely be 4 threads. Set to -1 to allow to grab everything.
        /// </summary>
        int ParallelDegree { get; }
    }
}