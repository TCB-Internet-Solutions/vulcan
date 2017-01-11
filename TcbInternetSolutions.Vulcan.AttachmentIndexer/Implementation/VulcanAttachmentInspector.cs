namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    using Core.Extensions;
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System.Configuration;
    using System.Linq;

    /// <summary>
    /// Determines if attachment can be indexed
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanAttachmentInspector), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentInspector : IVulcanAttachmentInspector
    {
        /// <summary>
        /// Determines if given mediadata is indexable
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public virtual bool AllowIndexing(MediaData media)
        {
            if (media == null)
                return false;
            
            var ext = media.SearchFileExtension();

            if (!string.IsNullOrWhiteSpace(ext))
            {
                var allowedExtensions = ConfigurationManager.AppSettings["VulcanIndexAttachmentFileExtensions"];

                if (string.IsNullOrWhiteSpace(allowedExtensions))
                    return false;

                var extensions = allowedExtensions.Split(new char[] { ',', '|', ';' }, System.StringSplitOptions.RemoveEmptyEntries);

                return extensions.Any(x => string.Compare(ext, x.Trim().TrimStart('.'), true) == 0);
            }

            // TODO: Should we also allow for a file size restriction via application setting?
            //long fileByteSize = -1;

            //if (media != null)
            //{
            //    using (var stream = media.BinaryData.OpenRead())
            //    {
            //        fileByteSize = stream.Length;
            //    }
            //}

            return false;
        }
    }
}
