namespace TcbInternetSolutions.Vulcan.Core
{
    using EPiServer.Core;
    using System.Collections.Generic;

    public interface IVulcanContentIndexer : IVulcanIndexer
    {
        KeyValuePair<ContentReference, string> GetRoot();
    }
}
