using EPiServer.Core;
using EPiServer.ServiceLocation;
using System;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core.Extensions;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    /// <summary>
    /// Determines if attachment can be indexed
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanAttachmentInspector), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentInspector : IVulcanAttachmentInspector
    {
        private readonly IVulcanAttachmentIndexerSettings _attachmentSettings;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="attachmentSettings"></param>
        public VulcanAttachmentInspector(IVulcanAttachmentIndexerSettings attachmentSettings)
        {
            _attachmentSettings = attachmentSettings;
        }

        /// <summary>
        /// Determines if given mediadata is indexable
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual bool AllowIndexing(MediaData media)
        {
            if (media == null || !_attachmentSettings.EnableAttachmentPlugins)
                return false;

            var allowed = true;
            var ext = media.SearchFileExtension();

            if (!string.IsNullOrWhiteSpace(ext))
            {
                allowed = _attachmentSettings?.SupportedFileExtensions?.Any(x => string.Compare(ext, x.Trim().TrimStart('.'), StringComparison.OrdinalIgnoreCase) == 0) == true;
            }

            if (!allowed || !_attachmentSettings.EnableFileSizeLimit) return allowed;
            long fileByteSize = -1;

            if (media.BinaryData != null)
            {
                using (var stream = media.BinaryData.OpenRead())
                {
                    fileByteSize = stream.Length;
                }
            }

            allowed = fileByteSize > 1 && _attachmentSettings.FileSizeLimit <= fileByteSize;

            return allowed;
        }
    }
}
