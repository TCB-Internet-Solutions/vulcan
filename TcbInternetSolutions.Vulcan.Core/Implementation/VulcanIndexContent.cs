namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using EPiServer.ServiceLocation;
    using Extensions;
    using System;
    using System.Linq;

    /// <summary>
    /// Default index job
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Vulcan Index Content")]
    public class VulcanIndexContent : ScheduledJobBase
    {
        private static ILogger Logger = LogManager.GetLogger();

        private bool _stopSignaled;

        /// <summary>
        /// Injected content loader
        /// </summary>
        public Injected<IContentLoader> ContentLoader { get; set; }

        /// <summary>
        /// Injected vulcan handler
        /// </summary>
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        /// <summary>
        /// Injected poco handler
        /// </summary>
        public Injected<IVulcanPocoIndexingJob> VulcanPocoIndexHandler { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanIndexContent()
        {
            IsStoppable = true;
        }

        /// <summary>
        /// Signal stop
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }

        /// <summary>
        /// Execute index job
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            OnStatusChanged(string.Format("Starting execution of {0}", GetType()));

            VulcanHandler.Service.DeleteIndex(); // delete all language indexes
            var indexers = typeof(IVulcanIndexer).GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);
            var count = 0;

            //indexers.AsParallel().ForAll((Type t) =>
            //{
            //    return;
            //});

            for (int i = 0; i < indexers.Count; i++)
            {
                var indexer = (IVulcanIndexer)Activator.CreateInstance(indexers[i]);
                var pocoIndexer = indexer as IVulcanPocoIndexer;
                var cmsIndexer = indexer as IVulcanContentIndexer;

                if (pocoIndexer?.IncludeInDefaultIndexJob == true)
                {
                    VulcanPocoIndexHandler.Service.Index(pocoIndexer, OnStatusChanged, ref count, ref _stopSignaled);
                }
                else if(cmsIndexer != null) // default episerver content
                {
                    var contentReferences = ContentLoader.Service.GetDescendents(cmsIndexer.GetRoot().Key);

                    for (int cr = 0; cr < contentReferences.Count(); cr++)
                    {
                        OnStatusChanged(indexer.IndexerName + " indexing item " + (cr + 1).ToString() + " of " + contentReferences.Count() + " items of " + cmsIndexer.GetRoot().Value + " content (indexer " + (i + 1).ToString() + " of " + indexers.Count.ToString() + ")...");

                        VulcanHandler.Service.IndexContentEveryLanguage(ContentLoader.Service.Get<IContent>(contentReferences.ElementAt(cr)));

                        if (_stopSignaled)
                        {
                            return "Stop of job was called";
                        }

                        count++;
                    }
                }
            }

            return "Vulcan successfully indexed " + count.ToString() + " item(s)";
        }
    }
}
