using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    public interface IVulcanAttachmentInspector
    {
        bool AllowIndexing(MediaData media);
    }
}
