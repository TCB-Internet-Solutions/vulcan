using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    /// <summary>
    /// Determines if attachment can be indexed
    /// </summary>
    public interface IVulcanAttachmentInspector
    {
        /// <summary>
        /// Determines if given mediadata is indexable
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        bool AllowIndexing(MediaData media);
    }
}
