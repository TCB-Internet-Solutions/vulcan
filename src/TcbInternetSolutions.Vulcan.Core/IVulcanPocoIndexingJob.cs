namespace TcbInternetSolutions.Vulcan.Core
{
    using System;

    /// <summary>
    /// Vulcan POCO indexing job
    /// </summary>
    public interface IVulcanPocoIndexingJob
    {
        /// <summary>
        /// Indexer job
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="updateStatus"></param>
        /// <param name="count"></param>
        /// <param name="stopSignaled"></param>
        /// <returns></returns>
        string Index(IVulcanPocoIndexer pocoIndexer, Action<string> updateStatus, ref int count, ref bool stopSignaled, string alias = null);

        /// <summary>
        /// Index item
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="item"></param>
        void IndexItem(IVulcanPocoIndexer pocoIndexer, object item, string alias = null);

        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="pocoIndexer"></param>
        /// <param name="item"></param>
        void DeleteItem(IVulcanPocoIndexer pocoIndexer, object item, string alias = null);
    }
}