using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Elastic Search Pipeline
    /// </summary>
    public interface IVulcanPipeline
    {
        /// <summary>
        /// Pipeline name, no spaces or special characters please
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Sort to determine pipeline, highest wins
        /// </summary>
        int SortOrder { get; }

        /// <summary>
        /// Determines if content needs a pipeline
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        bool IsMatch(IContent content);
    }
}
