namespace TcbInternetSolutions.Vulcan.Core
{
    using EPiServer.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Indexer for Episerver content
    /// </summary>
    public interface IVulcanContentIndexer : IVulcanIndexer
    {
        /// <summary>
        /// Root reference to index descendents
        /// </summary>
        /// <returns></returns>
        KeyValuePair<ContentReference, string> GetRoot();

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
