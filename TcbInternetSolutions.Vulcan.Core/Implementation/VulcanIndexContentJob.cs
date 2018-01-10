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
        private static ILogger _Logger = LogManager.GetLogger();
        private readonly IVulcanHandler _VulcanHandler;
        private readonly IVulcanPocoIndexingJob _VulcanPocoIndexHandler;
        private readonly IVulcanSearchContentLoader _VulcanSearchContentLoader;
        private readonly IEnumerable<IVulcanIndexer> _VulcanIndexers;
        private bool _StopSignaled;

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
        ) : base()
        {
            _VulcanSearchContentLoader = vulcanSearchContentLoader;
            _VulcanHandler = vulcanHandler;
            _VulcanPocoIndexHandler = vulcanPocoIndexingJob;
            _VulcanIndexers = vulcanIndexers;
            IsStoppable = true;
        }

        /// <summary>
        /// Execute index job
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            OnStatusChanged($"Starting execution of {GetType()}");
            _VulcanHandler.DeleteIndex(); // delete all language indexes
            var totalIndexedCount = 0;

            foreach (var indexer in _VulcanIndexers)
            {
                var pocoIndexer = indexer as IVulcanPocoIndexer;
                var cmsIndexer = indexer as IVulcanContentIndexer;

                if (pocoIndexer?.IncludeInDefaultIndexJob == true)
                {
                    _VulcanPocoIndexHandler.Index(pocoIndexer, OnStatusChanged, ref totalIndexedCount, ref _StopSignaled);
                }
                else if (cmsIndexer != null) // default episerver content
                {
                    var contentReferences = _VulcanSearchContentLoader.GetSearchContentReferences(cmsIndexer).ToList();

                    for (int cr = 0; cr < contentReferences.Count; cr++)
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
                            OnStatusChanged($"{indexer.IndexerName} indexing item {(cr + 1)} of {contentReferences.Count} items of {cmsIndexer.GetRoot().Value} content");
                        }

                        IContent content = null;
                        ContentReference contentReference = contentReferences.ElementAt(cr);

                        try
                        {
                            content = _VulcanSearchContentLoader.GetContent(contentReference);
                        }
                        catch (OutOfMemoryException)
                        {
                            _Logger.Warning($"Vulcan encountered an OutOfMemory exception, attempting again to index content item {contentReference}...");

                            // try once more
                            try
                            {
                                content = _VulcanSearchContentLoader.GetContent(contentReference);
                            }
                            catch (Exception eNested)
                            {
                                _Logger.Error($"Vulcan could not recover from an out of memory exception when it tried again to index content item  {contentReference} : {eNested}");
                            }
                        }
                        catch (Exception eOther)
                        {
                            _Logger.Error($"Vulcan could not index content item {contentReference} : {eOther}");
                        }

                        if (content == null)
                        {
                            _Logger.Error($"Vulcan could not index content item {contentReference}: content was null");
                        }
                        else
                        {
                            _Logger.Information($"Vulcan indexed content with reference: {cr} and name: {content.Name}");
                            _VulcanHandler.IndexContentEveryLanguage(content);
                            content = null; // dispose
                            totalIndexedCount++;
                        }

                        if (_StopSignaled)
                        {
                            return "Stop of job was called";
                        }
                    }
                }
            }

            return $"Vulcan successfully indexed {totalIndexedCount} item(s) across {_VulcanIndexers.Count()} indexers!";
        }

        /// <summary>
        /// Signal stop
        /// </summary>
        public override void Stop()
        {
            _StopSignaled = true;
        }
    }
}