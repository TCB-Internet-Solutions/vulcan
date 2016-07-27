namespace TcbInternetSolutions.Vulcan.Core
{
    using System.Collections.Generic;

    public interface IVulcanPocoIndexer : IVulcanIndexer
    {
        /// <summary>
        /// Total items to index
        /// </summary>
        long TotalItems { get; }

        /// <summary>
        /// Determines number of items to index at a time.
        /// </summary>
        int PageSize { get; }        

        /// <summary>
        /// Gets a list of items given page and pagesize.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        IEnumerable<object> GetItems(int page, int pageSize);

        /// <summary>
        /// Gets the ID of an item
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        string GetItemIdentifier(object o);

        /// <summary>
        /// Allows dev to choose if default index job will index the poco indexer data.
        /// </summary>
        bool IncludeInDefaultIndexJob { get; }
    }
}