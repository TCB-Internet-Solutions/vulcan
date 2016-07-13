namespace TcbInternetSolutions.Vulcan.Core
{
    using System.Collections.Generic;

    public interface IVulcanPocoIndexer : IVulcanIndexer
    {
        long TotalItems { get; }

        int PageSize { get; }        

        IEnumerable<object> GetItems(int page, int pageSize);

        string GetItemIdentifier(object o);
    }
}
