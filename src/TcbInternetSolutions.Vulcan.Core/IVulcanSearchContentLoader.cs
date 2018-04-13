using EPiServer.Core;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Loads content for Vulcan scheduled job
    /// </summary>
    public interface IVulcanSearchContentLoader
    {
        /// <summary>
        /// Gets all ContentReference of items to index.
        /// </summary>
        /// <param name="contentIndexer"></param>
        /// <returns></returns>
        IEnumerable<ContentReference> GetSearchContentReferences(IVulcanContentIndexer contentIndexer);
        
        /// <summary>
        /// Loads content by given content reference.
        /// </summary>
        /// <param name="contentLink"></param>
        /// <returns></returns>
        IContent GetContent(ContentReference contentLink);
    }
}