namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanPocoIndexingJob
    {
        void Index(IVulcanPocoIndexer pocoIndexer, ref int count);
    }
}
