using EPiServer.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanCmsIndexer : IVulcanIndexer
    {
        public KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot()
        {
            return new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");
        }
    }
}
