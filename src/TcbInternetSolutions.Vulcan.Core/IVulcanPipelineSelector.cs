using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Responsible for examining content to find a pipeline for indexing
    /// </summary>
    public interface IVulcanPipelineSelector
    {
        /// <summary>
        /// Returns a pipeline or null if a pipeline doesn't match the content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        IVulcanPipeline GetPipelineForContent(IContent content);

        /// <summary>
        /// Returns a pipeline for given ID, used during custom serialization
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IVulcanPipeline GetPipelineById(string id);
    }
}
