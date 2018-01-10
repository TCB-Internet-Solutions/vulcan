using EPiServer.Core;
using System.Collections.Generic;
using System.IO;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Index modifier
    /// </summary>
    public interface IVulcanIndexingModifier
    {
        /// <summary>
        /// Process modifier and flush customization to stream
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        void ProcessContent(IContent content, Stream writableStream);
    }

    /// <summary>
    /// Indexing modifier that supports ancestors
    /// </summary>
    public interface IVulcanIndexingModifierWithAncestors : IVulcanIndexingModifier
    {
        /// <summary>
        /// Gets ancestors for given content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        IEnumerable<ContentReference> GetAncestors(IContent content);
    }
}
