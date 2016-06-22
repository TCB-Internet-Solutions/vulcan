using EPiServer.Core;
using System.IO;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexingModifier
    {
        void ProcessContent(IContent content, Stream writableStream);
    }
}
