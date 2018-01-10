using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Setup events for content syncs to search
    /// </summary>
    [InitializableModule]
    public class VulcanIndexSynchronization : IInitializableModule
    {
        IContentEvents _ContentEvents;
        IContentRepository _ContentRepository;
        IVulcanHandler _VulcanHandler;

        /// <summary>
        /// Assigns private variables, and wires up content events
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(InitializationEngine context)
        {
            _ContentEvents = context.Locate.ContentEvents();
            _ContentRepository = context.Locate.ContentRepository();
            _VulcanHandler = context.Locate.Advanced.GetInstance<IVulcanHandler>();

            // TODO: Add content events to work with content area attribute to sync block changes that are searchable.

            _ContentEvents.PublishedContent += Service_PublishedContent;
            _ContentEvents.MovedContent += Service_MovedContent;
            _ContentEvents.DeletedContent += Service_DeletedContent;
            _ContentEvents.DeletedContentLanguage += Service_DeletedContentLanguage;
        }

        /// <summary>
        /// Un-init event
        /// </summary>
        /// <param name="context"></param>
        public void Uninitialize(InitializationEngine context)
        {
            _ContentEvents.PublishedContent -= Service_PublishedContent;
            _ContentEvents.MovedContent -= Service_MovedContent;
            _ContentEvents.DeletedContent -= Service_DeletedContent;
            _ContentEvents.DeletedContentLanguage -= Service_DeletedContentLanguage;
        }

        void Service_DeletedContent(object sender, EPiServer.DeleteContentEventArgs e)
        {
            _VulcanHandler.DeleteContentEveryLanguage(e.ContentLink);
        }

        void Service_DeletedContentLanguage(object sender, EPiServer.ContentEventArgs e)
        {
            _VulcanHandler.DeleteContentByLanguage(e.Content);
        }

        void Service_MovedContent(object sender, EPiServer.ContentEventArgs e)
        {
            if (e.TargetLink.CompareToIgnoreWorkID(SiteDefinition.Current.WasteBasket))
            {
                _VulcanHandler.DeleteContentEveryLanguage(e.ContentLink);
            }
            else
            {
                _VulcanHandler.IndexContentEveryLanguage(e.Content);
            }
        }

        void Service_PublishedContent(object sender, EPiServer.ContentEventArgs e)
        {
            // Update the index for the currently published content
            _VulcanHandler.IndexContentByLanguage(e.Content);

            // See if there are references to the content and if so, update the index for them as well
            var references = _ContentRepository.GetReferencesToContent(e.ContentLink, false);

            foreach (var r in references.Where(x => !x.OwnerID.CompareToIgnoreWorkID(e.ContentLink)))
            {
                _VulcanHandler.IndexContentByLanguage(_ContentRepository.Get<IContent>(r.OwnerID));
            }

        }
    }
}