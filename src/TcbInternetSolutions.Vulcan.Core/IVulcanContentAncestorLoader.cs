using EPiServer.Core;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Indexing modifier that supports ancestors
    /// </summary>
    public interface IVulcanContentAncestorLoader
    {
        /// <summary>
        /// Gets ancestors for given content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        IEnumerable<ContentReference> GetAncestors(IContent content);
    }
}
