namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Default index job
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Vulcan Index Content")]
    public class VulcanIndexContentJob : ScheduledJobBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IVulcanHandler _vulcanHandler;
        private readonly IVulcanPocoIndexingJob _vulcanPocoIndexHandler;
        private readonly IVulcanSearchContentLoader _vulcanSearchContentLoader;
        private readonly IEnumerable<IVulcanIndexer> _vulcanIndexers;
        private bool _stopSignaled;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanSearchContentLoader"></param>
        /// <param name="vulcanHandler"></param>
        /// <param name="vulcanPocoIndexingJob"></param>
        /// <param name="vulcanIndexers"></param>
        public VulcanIndexContentJob
        (
            IVulcanSearchContentLoader vulcanSearchContentLoader,
            IVulcanHandler vulcanHandler,
            IVulcanPocoIndexingJob vulcanPocoIndexingJob,
            IEnumerable<IVulcanIndexer> vulcanIndexers
        )
        {
            _vulcanSearchContentLoader = vulcanSearchContentLoader;
            _vulcanHandler = vulcanHandler;
            _vulcanPocoIndexHandler = vulcanPocoIndexingJob;
            _vulcanIndexers = vulcanIndexers;
            IsStoppable = true;
        }

        /// <summary>
        /// Execute index job
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            OnStatusChanged($"Starting execution of {GetType()}");
            _vulcanHandler.DeleteIndex(); // delete all language indexes
            var totalIndexedCount = 0;

            foreach (var indexer in _vulcanIndexers)
            {
                var pocoIndexer = indexer as IVulcanPocoIndexer;
                var cmsIndexer = indexer as IVulcanContentIndexer;

                if (pocoIndexer?.IncludeInDefaultIndexJob == true)
                {
                    _vulcanPocoIndexHandler.Index(pocoIndexer, OnStatusChanged, ref totalIndexedCount, ref _stopSignaled);
                }
                else if (cmsIndexer != null) // default episerver content
                {
                    var contentReferences = _vulcanSearchContentLoader.GetSearchContentReferences(cmsIndexer).ToList();

                    for (var cr = 0; cr < contentReferences.Count; cr++)
                    {
                        if (cmsIndexer.ClearCacheItemInterval >= 0)
                        {
                            if (cr % cmsIndexer.ClearCacheItemInterval == 0)
                            {
                                cmsIndexer.ClearCache();
                            }
                        }

                        // only update this every 100 records (reduce load on db)
                        if (cr % 100 == 0)
                        {
                            OnStatusChanged($"{indexer.IndexerName} indexing item {cr + 1} of {contentReferences.Count} items of {cmsIndexer.GetRoot().Value} content");
                        }

                        IContent content = null;
                        var contentReference = contentReferences.ElementAt(cr);

                        try
                        {
                            content = _vulcanSearchContentLoader.GetContent(contentReference);
                        }
                        catch (OutOfMemoryException)
                        {
                            Logger.Warning($"Vulcan encountered an OutOfMemory exception, attempting again to index content item {contentReference}...");

                            // try once more
                            try
                            {
                                content = _vulcanSearchContentLoader.GetContent(contentReference);
                            }
                            catch (Exception eNested)
                            {
                                Logger.Error($"Vulcan could not recover from an out of memory exception when it tried again to index content item  {contentReference} : {eNested}");
                            }
                        }
                        catch (Exception eOther)
                        {
                            Logger.Error($"Vulcan could not index content item {contentReference} : {eOther}");
                        }

                        if (content == null)
                        {
                            Logger.Error($"Vulcan could not index content item {contentReference}: content was null");
                        }
                        else
                        {
                            Logger.Information($"Vulcan indexed content with reference: {cr} and name: {content.Name}");
                            _vulcanHandler.IndexContentEveryLanguage(content);
                            totalIndexedCount++;
                        }

                        if (_stopSignaled)
                        {
                            return "Stop of job was called";
                        }
                    }
                }
            }

            return $"Vulcan successfully indexed {totalIndexedCount} item(s) across {_vulcanIndexers.Count()} indexers!";
        }

        /// <summary>
        /// Signal stop
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}