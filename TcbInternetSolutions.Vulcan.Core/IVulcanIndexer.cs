using EPiServer.Core;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexer
    {
        KeyValuePair<ContentReference, string> GetRoot();
    }
}
