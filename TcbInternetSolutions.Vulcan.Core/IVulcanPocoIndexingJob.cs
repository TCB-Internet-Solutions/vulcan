namespace TcbInternetSolutions.Vulcan.Core
{
    using System;

    public interface IVulcanPocoIndexingJob
    {
        string Index(IVulcanPocoIndexer pocoIndexer, Action<string> updateStatus, ref int count, ref bool stopSignaled);

        void IndexItem(IVulcanPocoIndexer pocoIndexer, object item);

        void DeleteItem(IVulcanPocoIndexer pocoIndexer, object item);
    }
}