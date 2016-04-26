using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanIndexer
    {
        KeyValuePair<ContentReference, string> GetRoot();
    }
}
