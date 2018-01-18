namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
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
        private static ILogger Logger = LogManager.GetLogger(typeof(VulcanPocoIndexingJob));

        /// <summary>
        /// invariant client
        /// </summary>
        protected IVulcanClient _InvariantClient => _VulcanHander.GetClient(CultureInfo.InvariantCulture);

        /// <summary>
        /// Vulcan handler
        /// </summary>
        protected IVulcanHandler _VulcanHander;

        /// <summary>
        /// Injected constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        public VulcanPocoIndexingJob(IVulcanHandler vulcanHandler)
        {
            _VulcanHander = vulcanHandler;
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
                var response = _InvariantClient.Delete(new DeleteRequest(_InvariantClient.IndexName, type, id));
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

            for (int page = 1; page <= totalPages; page++)
            {
                updateStatus?.Invoke("Indexing page " + page + " of " + totalPages + " items of " + pocoIndexer.IndexerName + " content!");                
                var itemsToIndex = pocoIndexer.GetItems(page , pageSize);
                var firstItem = itemsToIndex.FirstOrDefault();

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

                    var indexItem = Activator.CreateInstance(operationType, item) as IBulkOperation;
                    indexItem.Type = new TypeName() { Name = itemTypeName, Type = itemType };
                    indexItem.Id = pocoIndexer.GetItemIdentifier(item);
                    operations.Add(indexItem);

                    internalCount++;
                    count++;
                }

                // https://www.elastic.co/guide/en/elasticsearch/client/net-api/1.x/bulk.html
                var request = new BulkRequest()
                {                    
                    // todo: nest 5 to 2 difference
                    Refresh = true,// Refresh.True,
                    //Consistency = Consistency.One, // removed in nest 5
                    Operations = operations
                };

                var response = _InvariantClient.Bulk(request);
            }

            return "Indexed " + internalCount + " of " + total + " items of " + pocoIndexer.IndexerName + " content!";
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
                var response =  _InvariantClient.Index(item, z => z.Id(id).Type(type));
                Logger.Debug("Vulcan indexed " + id + " for type " + type + ": " + response.DebugInformation);
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not index object of type " + type + " with ID " + id, e);
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