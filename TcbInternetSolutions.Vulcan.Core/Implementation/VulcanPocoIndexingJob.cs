namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using Elasticsearch.Net;
    using EPiServer.ServiceLocation;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    [ServiceConfiguration(typeof(IVulcanPocoIndexingJob), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanPocoIndexingJob : IVulcanPocoIndexingJob
    {
        private IVulcanHandler _VulcanHander;

        public VulcanPocoIndexingJob() : this(ServiceLocator.Current.GetInstance<IVulcanHandler>()){ }

        public VulcanPocoIndexingJob(IVulcanHandler vulcanHandler)
        {
            _VulcanHander = vulcanHandler;
        }

        public virtual string Index(IVulcanPocoIndexer pocoIndexer, Action<string> updateStatus, ref int count, ref bool stopSignaled)
        {
            if (pocoIndexer == null)
                throw new ArgumentNullException($"{nameof(pocoIndexer)} cannot be null!");

            var invariantClient = _VulcanHander.GetClient(CultureInfo.InvariantCulture);
            var total = pocoIndexer.TotalItems;
            var pageSize = pocoIndexer.PageSize;
            pageSize = pageSize < 1 ? 1 : pageSize; // don't allow 0 or negative
            var totalPages = (total + pageSize - 1) / pageSize;
            var internalCount = 0;

            for (int page = 0; page < totalPages; page++)
            {
                updateStatus?.Invoke("Indexing page " + page + 1 + " of " + totalPages + " items of " + pocoIndexer.IndexerName + " content!");                
                var itemsToIndex = pocoIndexer.GetItems(page, pageSize);
                var firstItem = itemsToIndex.FirstOrDefault();

                if (firstItem == null)
                    break;
                
                var itemType = firstItem.GetType();
                var itemTypeName = itemType.FullName;
                var operationType = typeof(BulkIndexOperation<>).MakeGenericType(itemType);
                var operations = new List<IBulkOperation>();

                foreach (var item in itemsToIndex)
                {
                    // index request per item
                    //invariantClient.Index(item, z => z.Id(pocoIndexer.GetItemIdentifier(item)).Type(item.GetType().FullName));

                    if (stopSignaled)
                    {
                        return "Stop of job was called";
                    }

                    var indexItem = Activator.CreateInstance(operationType, item) as IBulkOperation;
                    indexItem.Type = new TypeName() { Name = itemTypeName, Type = itemType };
                    operations.Add(indexItem);

                    internalCount++;
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

            return "Indexed " + internalCount + " of " + total + " items of " + pocoIndexer.IndexerName + " content!";
        }
    }
}