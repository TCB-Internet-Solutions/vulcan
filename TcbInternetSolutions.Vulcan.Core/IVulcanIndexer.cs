using EPiServer.Core;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexer
    {
        KeyValuePair<ContentReference, string> GetRoot();
    }

    public interface IVulcanPocoIndexer : IVulcanIndexer
    {
        long TotalItems { get; }

        int PageSize { get; }

        string IndexerName { get; }

        IEnumerable<object> GetItems(int page, int pageSize);

        string GetItemIdentifier(object o);
    }
}
