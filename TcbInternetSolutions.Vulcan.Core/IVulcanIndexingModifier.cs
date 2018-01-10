using EPiServer.Core;
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
}
