using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexingModifier
    {
        void ProcessContent(IContent content, Stream writableStream);
    }
}
