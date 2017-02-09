namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    using Core.Extensions;
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System.Linq;

    /// <summary>
    /// Determines if attachment can be indexed
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanAttachmentInspector), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentInspector : IVulcanAttachmentInspector
    {
        IVulcanAttachmentIndexerSettings _AttachmentSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="attachmentSettings"></param>
        public VulcanAttachmentInspector(IVulcanAttachmentIndexerSettings attachmentSettings)
        {
            _AttachmentSettings = attachmentSettings;
        }

        /// <summary>
        /// Determines if given mediadata is indexable
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual bool AllowIndexing(MediaData media)
        {
            if (media == null)
                return false;

            bool allowed = true;
            var ext = media.SearchFileExtension();

            if (!string.IsNullOrWhiteSpace(ext))
            {
                allowed = _AttachmentSettings?.SupportedFileExtensions?.Any(x => string.Compare(ext, x.Trim().TrimStart('.'), true) == 0) == true;
            }

            if (allowed && _AttachmentSettings.EnableFileSizeLimit)
            {
                long fileByteSize = -1;

                if (media?.BinaryData != null)
                {
                    using (var stream = media.BinaryData.OpenRead())
                    {
                        fileByteSize = stream.Length;
                    }
                }

                allowed = _AttachmentSettings.FileSizeLimit <= fileByteSize;
            }

            return allowed;
        }
    }
}
