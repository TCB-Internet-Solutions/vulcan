using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    /// <summary>
    /// Settings for vulcan attachment indexing
    /// </summary>
    public interface IVulcanAttachmentIndexerSettings
    {
        /// <summary>
        /// If true, the Elasticsearch server must have mapper-attachments (2.x) or ingest-attachments (5.x) installed
        /// </summary>
        bool EnableAttachmentPlugins { get; }
        
        /// <summary>
        /// Determines supported file extensions for indexing.
        /// </summary>
        IEnumerable<string> SupportedFileExtensions { get; }

        /// <summary>
        /// Determines if file size limits are used to determine indexing
        /// </summary>
        bool EnableFileSizeLimit { get; }

        /// <summary>
        /// Determines max file size of media to index
        /// </summary>
        long FileSizeLimit { get; }
    }
}
