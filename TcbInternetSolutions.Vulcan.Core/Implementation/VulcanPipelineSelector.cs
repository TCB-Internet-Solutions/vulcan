namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Default Pipeline Selector
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanPipelineSelector), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanPipelineSelector : IVulcanPipelineSelector
    {
        private readonly IEnumerable<IVulcanPipeline> _AllPipelines;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="allPipelines"></param>
        public VulcanPipelineSelector(IEnumerable<IVulcanPipeline> allPipelines)
        {
            _AllPipelines = allPipelines;
        }

        /// <summary>
        /// Tries to return pipeline for given Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual IVulcanPipeline GetPipelineById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return _AllPipelines.FirstOrDefault(x => string.Compare(id, x.Id, System.StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Tries to determine if pipeline is a match for given content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public virtual IVulcanPipeline GetPipelineForContent(IContent content)
        {
            if (_AllPipelines?.Any() == true)
            {
                foreach (var pipeline in _AllPipelines.OrderByDescending(x => x.SortOrder))
                {
                    if (pipeline.IsMatch(content))
                    {
                        return pipeline;
                    }
                }
            }

            return null;
        }
    }
}
