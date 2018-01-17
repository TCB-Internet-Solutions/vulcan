using EPiServer.Core;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Arguments used to modify indexing
    /// </summary>
    public interface IVulcanIndexingModifierArgs
    {
        /// <summary>
        /// Content Instance
        /// </summary>
        IContent Content { get; }

        /// <summary>
        /// Matched pipeline Id or null
        /// </summary>
        string PipelineId { get; }

        /// <summary>
        /// Additional items to serialize for item
        /// </summary>
        IDictionary<string, object> AdditionalItems { get; }
    }
}