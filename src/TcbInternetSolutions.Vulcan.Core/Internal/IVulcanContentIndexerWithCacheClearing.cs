namespace TcbInternetSolutions.Vulcan.Core.Internal
{
    /// <summary>
    /// Indexer that can clear cache, temporary until full migration of using cache scoping
    /// </summary>
    public interface IVulcanContentIndexerWithCacheClearing : IVulcanContentIndexer
    {
        /// <summary>
        /// Clear cache every X items
        /// </summary>
        int ClearCacheItemInterval { get; }

        /// <summary>
        /// Clears any cache - so keeping performance high
        /// </summary>
        void ClearCache();
    }
}