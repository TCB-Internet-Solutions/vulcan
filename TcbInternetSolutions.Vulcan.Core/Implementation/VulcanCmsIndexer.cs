using EPiServer.Web;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanCmsIndexer : IVulcanIndexer
    {
        public virtual KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");
    }
}
