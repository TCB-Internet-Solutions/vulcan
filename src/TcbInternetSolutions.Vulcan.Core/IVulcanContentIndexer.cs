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
    }
}
