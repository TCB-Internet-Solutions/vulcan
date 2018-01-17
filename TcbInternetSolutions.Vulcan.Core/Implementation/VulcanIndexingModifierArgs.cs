namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using System.Collections.Generic;
    using EPiServer.Core;

    /// <summary>
    /// Arguments used to modify indexing
    /// </summary>
    public class VulcanIndexingModifierArgs : IVulcanIndexingModifierArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="content"></param>
        /// <param name="pipelineId"></param>
        public VulcanIndexingModifierArgs(IContent content, string pipelineId)
        {
            Content = content;
            PipelineId = pipelineId;
            AdditionalItems = new Dictionary<string, object>();            
        }

        /// <summary>
        /// Content Instance
        /// </summary>
        public IContent Content { get; }

        /// <summary>
        /// Matched pipeline Id or null
        /// </summary>
        public string PipelineId { get; }

        /// <summary>
        /// Additional serialization items
        /// </summary>
        public IDictionary<string, object> AdditionalItems { get; }
    }
}
