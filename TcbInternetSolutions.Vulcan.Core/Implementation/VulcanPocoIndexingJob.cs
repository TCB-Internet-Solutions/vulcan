namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using Elasticsearch.Net;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Poco indexing job
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanPocoIndexingJob), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanPocoIndexingJob : IVulcanPocoIndexingJob
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(VulcanPocoIndexingJob));

        /// <summary>
        /// invariant client
        /// </summary>
        protected IVulcanClient InvariantClient => VulcanHander.GetClient(CultureInfo.InvariantCulture);

        /// <summary>
        /// Vulcan handler
        /// </summary>
        protected IVulcanHandler VulcanHander;

        /// <summary>
        /// Injected constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        public VulcanPocoIndexingJob(IVulcanHandler vulcanHandler)
        {
            VulcanHander = vulcanHandler;
        }

        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="item"></param>
        public virtual void DeleteItem(IVulcanPocoIndexer pocoIndexer, object item)
        {
            var id = pocoIndexer.GetItemIdentifier(item);
            var type = GetTypeName(item);

            try
            {
                var response = InvariantClient.Delete(new DeleteRequest(InvariantClient.IndexName, type, id));
                Logger.Debug("Vulcan deleted " + id + " for type " + type + ": " + response.DebugInformation);
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not delete object of type " + type + " with ID " + id, e);
            }
        }

        /// <summary>
        /// Index item
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="updateStatus"></param>
        /// <param name="count"></param>
        /// <param name="stopSignaled"></param>
        /// <returns></returns>
        public virtual string Index(IVulcanPocoIndexer pocoIndexer, Action<string> updateStatus, ref int count, ref bool stopSignaled)
        {
            if (pocoIndexer == null)
                throw new ArgumentNullException($"{nameof(pocoIndexer)} cannot be null!");

            var total = pocoIndexer.TotalItems;
            var pageSize = pocoIndexer.PageSize;
            pageSize = pageSize < 1 ? 1 : pageSize; // don't allow 0 or negative
            var totalPages = (total + pageSize - 1) / pageSize;
            var internalCount = 0;
            
            for (var page = 1; page <= totalPages; page++)
            {
                updateStatus?.Invoke($"Indexing page {page} of {totalPages} items of {pocoIndexer.IndexerName} content!");
                var itemsToIndex = pocoIndexer.GetItems(page, pageSize)?.ToList();
                var firstItem = itemsToIndex?.FirstOrDefault();

                if (firstItem == null)
                    break;

                var itemType = firstItem.GetType();
                var itemTypeName = GetTypeName(firstItem);
                var operationType = typeof(BulkIndexOperation<>).MakeGenericType(itemType);
                var operations = new List<IBulkOperation>();

                foreach (var item in itemsToIndex)
                {
                    if (stopSignaled)
                    {
                        return "Stop of job was called";
                    }

                    if (!(Activator.CreateInstance(operationType, item) is IBulkOperation indexItem))
                        throw new Exception("Unable to create item for bulk request");

                    indexItem.Type = new TypeName { Name = itemTypeName, Type = itemType };
                    indexItem.Id = pocoIndexer.GetItemIdentifier(item);
                    operations.Add(indexItem);

                    internalCount++;
                    count++;
                }

                // https://www.elastic.co/guide/en/elasticsearch/client/net-api/1.x/bulk.html
                var request = new BulkRequest
                {
#if NEST2
                    Refresh = true,
                    Consistency = Consistency.One,
#elif NEST5
                    Refresh = Refresh.True,
#endif
                    Operations = operations
                };

                InvariantClient.Bulk(request);
            }

            return $"Indexed {internalCount} of {total} items of {pocoIndexer.IndexerName} content!";
        }

        /// <summary>
        /// Index item
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="item"></param>
        public virtual void IndexItem(IVulcanPocoIndexer pocoIndexer, object item)
        {
            var id = pocoIndexer.GetItemIdentifier(item);
            var type = GetTypeName(item);

            try
            {
                var response = InvariantClient.Index(item, z => z.Id(id).Type(type));
                Logger.Debug($"Vulcan indexed {id} for type {type}: {response.DebugInformation}");
            }
            catch (Exception e)
            {
                Logger.Warning($"Vulcan could not index object of type {type} with ID {id}", e);
            }
        }

        /// <summary>
        /// Gets typename for poco
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        protected virtual string GetTypeName(object o) => o.GetType().FullName;
    }
}