using EPiServer.Web;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default CMS content indexer
    /// </summary>
    public class VulcanCmsIndexer : IVulcanContentIndexer
    {
        /// <summary>
        /// Indexer Name
        /// </summary>
        public string IndexerName => "CMS Content";

        /// <summary>
        /// Indexer root
        /// </summary>
        /// <returns></returns>
        public virtual KeyValuePair<EPiServer.Core.ContentReference, string> GetRoot() =>
            new KeyValuePair<EPiServer.Core.ContentReference, string>(SiteDefinition.Current.RootPage, "CMS");
    }
}
