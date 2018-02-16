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
        private readonly IContentLoader _contentLoader;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentLoader"></param>
        public VulcanCmsContentAncestorLoader(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        /// <summary>
        /// Gets IContent ancestors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            return _contentLoader.GetAncestors(content.ContentLink)?.Select(c => c.ContentLink);
        }
    }
}
