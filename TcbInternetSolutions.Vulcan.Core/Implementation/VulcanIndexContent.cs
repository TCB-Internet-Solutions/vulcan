namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using Elasticsearch.Net;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using EPiServer.ServiceLocation;
    using Extensions;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    [ScheduledPlugIn(DisplayName = "Vulcan Index Content")]
    public class VulcanIndexContent : ScheduledJobBase
    {
        private static ILogger Logger = LogManager.GetLogger();

        private bool _stopSignaled;

        public Injected<IContentLoader> ContentLoader { get; set; }

        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public VulcanIndexContent()
        {
            IsStoppable = true;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            OnStatusChanged(string.Format("Starting execution of {0}", GetType()));
            
            VulcanHandler.Service.DeleteIndex(); // delete all language indexes
            var indexers = typeof(IVulcanIndexer).GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);
            var count = 0;

            for (int i = 0; i < indexers.Count; i++)
            {
                var indexer = (IVulcanIndexer)Activator.CreateInstance(indexers[i]);
                var pocoIndexer = indexer as IVulcanPocoIndexer;

                if (pocoIndexer != null)
                {
                    var invariantClient = VulcanHandler.Service.GetClient(CultureInfo.InvariantCulture);
                    long total = pocoIndexer.TotalItems;
                    int pageSize = pocoIndexer.PageSize;
                    var totalPages = (total + pageSize - 1) / pageSize;
                    
                    for (int page = 1; page <= totalPages; page++)
                    {
                        OnStatusChanged("Indexing page " + page + " of " + totalPages + " items of " + pocoIndexer.IndexerName + " content (indexer " + (i + 1).ToString() + " of " + indexers.Count.ToString() + ")...");

                        // TODO: Add POCO indexer to IVulcanHandler to batch..., store pocos in invariant index     
                        var itemsToIndex = pocoIndexer.GetItems(page, pageSize);                                           
                        var operations = new List<IBulkOperation>();
                        var itemType = itemsToIndex.FirstOrDefault()?.GetType();
                        var itemTypeName = itemType.FullName;
                        var operationType = typeof(BulkIndexOperation<>).MakeGenericType(itemType);

                        foreach (var item in itemsToIndex)
                        {
                            // index request per item
                            //invariantClient.Index(item,
                            //    z => z.Id(pocoIndexer.GetItemIdentifier(item)).Type(item.GetType().FullName));

                            if (_stopSignaled)
                            {
                                return "Stop of job was called";
                            }

                            var indexItem = Activator.CreateInstance(operationType, item) as IBulkOperation;
                            indexItem.Type = new TypeName() { Name = itemTypeName, Type = itemType };

                            operations.Add(indexItem);

                            count++;
                        }

                        // https://www.elastic.co/guide/en/elasticsearch/client/net-api/1.x/bulk.html
                        var request = new BulkRequest()
                        {
                            Refresh = true,
                            Consistency = Consistency.One,
                            Operations = operations
                        };

                        var response = invariantClient.Bulk(request);
                    }

                }
                else // default episerver content
                {
                    var contentReferences = ContentLoader.Service.GetDescendents(indexer.GetRoot().Key);

                    for (int cr = 0; cr < contentReferences.Count(); cr++)
                    {
                        OnStatusChanged("Indexing item " + (cr + 1).ToString() + " of " + contentReferences.Count() + " items of " + indexer.GetRoot().Value + " content (indexer " + (i + 1).ToString() + " of " + indexers.Count.ToString() + ")...");

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
