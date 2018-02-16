using EPiServer.Core;
using EPiServer.ServiceLocation;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    /// <summary>
    /// Determines if IContent enables the attachment pipeline
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanPipeline), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentPipeline : IVulcanPipeline
    {
        private readonly IVulcanAttachmentInspector _vulcanAttachmentInspector ;        

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanAttachmentInspector"></param>
        public VulcanAttachmentPipeline(IVulcanAttachmentInspector vulcanAttachmentInspector)
        {
            _vulcanAttachmentInspector = vulcanAttachmentInspector;            
        }

        /// <summary>
        /// Pipeline ID/name
        /// </summary>
        public string Id => VulcanAttachmentPipelineInstaller.PipelineId;

        /// <summary>
        /// Pipeline sort order
        /// </summary>
        public int SortOrder => 100;

        /// <summary>
        /// Determines if content matches pipeline
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool IsMatch(IContent content)
        {
            if (content is MediaData mediaContent)
            {
                return _vulcanAttachmentInspector.AllowIndexing(mediaContent);
            }

            return false;
        }
    }
}
