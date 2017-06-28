using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Default content loader for scheduled job
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanSearchContentLoader), Lifecycle = ServiceInstanceScope.Transient)]
    public class VulcanSearchContentLoader : IVulcanSearchContentLoader
    {
        private IContentLoader _ContentLoader;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contentLoader"></param>
        public VulcanSearchContentLoader(IContentLoader contentLoader)
        {
            _ContentLoader = contentLoader;
        }

        /// <summary>
        /// Loads content
        /// </summary>
        /// <param name="contentLink"></param>
        /// <returns></returns>
        public virtual IContent GetContent(ContentReference contentLink)
        {
            return _ContentLoader.Get<IContent>(contentLink);
        }

        /// <summary>
        /// Loads all root descendents by default
        /// </summary>
        /// <param name="contentIndexer"></param>
        /// <returns></returns>
        public virtual IEnumerable<ContentReference> GetSearchContentReferences(IVulcanContentIndexer contentIndexer)
        {
            return _ContentLoader.GetDescendents(contentIndexer.GetRoot().Key);
        }
    }
}
