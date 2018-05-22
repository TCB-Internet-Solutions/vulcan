using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TcbInternetSolutions.Vulcan.Core.Internal;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Index clear job
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Vulcan Index Clear", SortIndex = 1110, Description = "Use this job to clear ALL indexes for this site in Vulcan. This is useful for cleanup purposes but shouldn't need to be done regularly.")]
    public class VulcanIndexClearJob : ScheduledJobBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IVulcanHandler _vulcanHandler;
        private bool _stopSignaled;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        public VulcanIndexClearJob
        (
            IVulcanHandler vulcanHandler
        )
        {
            _vulcanHandler = vulcanHandler;
            IsStoppable = true;
        }

        /// <summary>
        /// Execute index clear job
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            OnStatusChanged($"Starting execution of {GetType()}");

            Logger.Warning("Clearing all indexes...");

            _vulcanHandler.DeleteIndex();

            Logger.Warning("All indexes cleared.");

            return $"Vulcan successfully cleared all indexes!";
        }
    }
}