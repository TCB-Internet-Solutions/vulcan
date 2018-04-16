using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core.Internal;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default index job
    /// </summary>
    [ScheduledPlugIn(DisplayName = "Vulcan Index Content")]
    public class VulcanIndexContentJob : ScheduledJobBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IEnumerable<IVulcanFeature> _vulcanFeatures;
        private readonly IVulcanHandler _vulcanHandler;
        private readonly IVulcanIndexContentJobSettings _vulcanIndexContentJobSettings;
        private readonly IEnumerable<IVulcanIndexer> _vulcanIndexers;
        private readonly IVulcanPocoIndexingJob _vulcanPocoIndexHandler;
        private readonly IVulcanSearchContentLoader _vulcanSearchContentLoader;
        private bool _stopSignaled;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanSearchContentLoader"></param>
        /// <param name="vulcanHandler"></param>
        /// <param name="vulcanPocoIndexingJob"></param>
        /// <param name="vulcanIndexers"></param>
        /// <param name="vulcanIndexContentJobSettings"></param>
        /// <param name="vulcanFeatures"></param>
        public VulcanIndexContentJob
        (
            IVulcanSearchContentLoader vulcanSearchContentLoader,
            IVulcanHandler vulcanHandler,
            IVulcanPocoIndexingJob vulcanPocoIndexingJob,
            IVulcanIndexContentJobSettings vulcanIndexContentJobSettings,
            IEnumerable<IVulcanIndexer> vulcanIndexers,
            IEnumerable<IVulcanFeature> vulcanFeatures
        )
        {
            _vulcanSearchContentLoader = vulcanSearchContentLoader;
            _vulcanHandler = vulcanHandler;
            _vulcanPocoIndexHandler = vulcanPocoIndexingJob;
            _vulcanIndexers = vulcanIndexers;
            _vulcanIndexContentJobSettings = vulcanIndexContentJobSettings;
            _vulcanFeatures = vulcanFeatures;
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
            var isCacheScopeFeature = _vulcanFeatures?.LastOrDefault(x => x is IVulcanFeatureCacheScope) as IVulcanFeatureCacheScope;

            foreach (var indexer in EnumerateIndexers())
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

                    var contentRecord = 0;
                    foreach (var contentReference in EnumerateContent(contentReferences))
                    {
                        if (isCacheScopeFeature?.Enabled != true &&
                            cmsIndexer is IVulcanContentIndexerWithCacheClearing cacheClearingIndexer && cacheClearingIndexer.ClearCacheItemInterval >= 0)
                        {
                            if (contentRecord % cacheClearingIndexer.ClearCacheItemInterval == 0)
                            {
                                cacheClearingIndexer.ClearCache();
                            }
                        }

                        // only update this every 100 records (reduce load on db)
                        if (contentRecord % 100 == 0)
                        {
                            OnStatusChanged($"{indexer.IndexerName} indexing item {contentRecord + 1} of {contentReferences.Count} items of {cmsIndexer.GetRoot().Value} content");
                        }

                        IContent content = null;

                        try
                        {
                            content = LoadWithCacheScope(contentReference, isCacheScopeFeature);
                        }
                        catch (OutOfMemoryException)
                        {
                            Logger.Warning($"Vulcan encountered an OutOfMemory exception, attempting again to index content item {contentReference}...");

                            // try once more
                            try
                            {
                                content = LoadWithCacheScope(contentReference, isCacheScopeFeature);
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
                            Logger.Information($"Vulcan indexed content with reference: {contentRecord} and name: {content.Name}");
                            _vulcanHandler.IndexContentEveryLanguage(content);
                            totalIndexedCount++;
                        }

                        if (_stopSignaled)
                        {
                            return "Stop of job was called";
                        }

                        contentRecord++;
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

        private IEnumerable<ContentReference> EnumerateContent(IEnumerable<ContentReference> contentReferences) =>
            _vulcanIndexContentJobSettings.EnableParallelContent ? contentReferences.AsParallel() : contentReferences;

        private IEnumerable<IVulcanIndexer> EnumerateIndexers() =>
            _vulcanIndexContentJobSettings.EnableParallelIndexers ? _vulcanIndexers.AsParallel() : _vulcanIndexers;

        private IContent LoadWithCacheScope(ContentReference c, IVulcanFeatureCacheScope vulcanFeatureCache)
        {
            if (vulcanFeatureCache?.Enabled != true) return _vulcanSearchContentLoader.GetContent(c);

            using (new ContentCacheScope { SlidingExpiration = vulcanFeatureCache.CacheDuration })
            {
                return _vulcanSearchContentLoader.GetContent(c);
            }
        }
    }
}