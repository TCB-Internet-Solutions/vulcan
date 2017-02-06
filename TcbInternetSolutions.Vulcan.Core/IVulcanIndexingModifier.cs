using EPiServer.Core;
using System.Collections.Generic;
using System.IO;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexingModifier
    {
        void ProcessContent(IContent content, Stream writableStream);

        IEnumerable<ContentReference> GetAncestors(IContent content);
    }
}
