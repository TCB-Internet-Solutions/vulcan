using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    /// <summary>
    /// Converts mediadata to byte array
    /// </summary>
    public interface IVulcanMediaReader
    {
        /// <summary>
        /// Converts given media data to byte array
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        byte[] ReadToEnd(MediaData media);
    }
}