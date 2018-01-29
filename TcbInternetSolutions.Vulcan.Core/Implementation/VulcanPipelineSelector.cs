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
        private readonly IEnumerable<IVulcanPipeline> _allPipelines;
        private IEnumerable<IVulcanPipeline> _sortedPipelines;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="allPipelines"></param>
        public VulcanPipelineSelector(IEnumerable<IVulcanPipeline> allPipelines)
        {
            _allPipelines = allPipelines;
        }

        /// <summary>
        /// Tries to return pipeline for given Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual IVulcanPipeline GetPipelineById(string id)
        {
            return string.IsNullOrWhiteSpace(id)
                ? null
                : GetSortedPipelines().FirstOrDefault(x =>
                    string.Compare(id, x.Id, System.StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Tries to determine if pipeline is a match for given content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public virtual IVulcanPipeline GetPipelineForContent(IContent content)
        {
            return GetSortedPipelines()?.FirstOrDefault(x => x.IsMatch(content));
        }

        private IEnumerable<IVulcanPipeline> GetSortedPipelines()
        {
            if (_sortedPipelines == null && _allPipelines?.Any() == true)
            {
                _sortedPipelines = _allPipelines.OrderByDescending(x => x.SortOrder).ToList();
            }

            return _sortedPipelines;
        }
    }
}