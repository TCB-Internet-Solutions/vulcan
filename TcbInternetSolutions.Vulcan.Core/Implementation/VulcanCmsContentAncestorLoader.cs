using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Gets ancestors for CMS content
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanContentAncestorLoader), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCmsContentAncestorLoader : IVulcanContentAncestorLoader
    {
        private readonly IContentLoader _ContentLoader;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentLoader"></param>
        public VulcanCmsContentAncestorLoader(IContentLoader contentLoader)
        {
            _ContentLoader = contentLoader;
        }

        /// <summary>
        /// Gets IContent ancestors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            return _ContentLoader.GetAncestors(content.ContentLink)?.Select(c => c.ContentLink);
        }
    }
}
