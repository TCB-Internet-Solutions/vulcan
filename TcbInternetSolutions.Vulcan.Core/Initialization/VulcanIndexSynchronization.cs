using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using TcbInternetSolutions.Vulcan.Core.Extensions;

namespace TcbInternetSolutions.Vulcan.Core.Initialization
{
    /// <summary>
    /// Setup events for content syncs to search
    /// </summary>
    [InitializableModule]
    public class VulcanIndexSynchronization : IInitializableModule
    {
        private IContentEvents _contentEvents;
        private IContentRepository _contentRepository;
        private IVulcanHandler _vulcanHandler;

        /// <summary>
        /// Assigns private variables, and wires up content events
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(InitializationEngine context)
        {
            _contentEvents = context.Locate.ContentEvents();
            _contentRepository = context.Locate.ContentRepository();
            _vulcanHandler = context.Locate.Advanced.GetInstance<IVulcanHandler>();

            // todo: Add content events to work with content area attribute to sync block changes that are searchable.

            _contentEvents.PublishedContent += Service_PublishedContent;
            _contentEvents.MovedContent += Service_MovedContent;
            _contentEvents.DeletedContent += Service_DeletedContent;
            _contentEvents.DeletedContentLanguage += Service_DeletedContentLanguage;
        }

        /// <summary>
        /// Un-init event
        /// </summary>
        /// <param name="context"></param>
        public void Uninitialize(InitializationEngine context)
        {
            _contentEvents.PublishedContent -= Service_PublishedContent;
            _contentEvents.MovedContent -= Service_MovedContent;
            _contentEvents.DeletedContent -= Service_DeletedContent;
            _contentEvents.DeletedContentLanguage -= Service_DeletedContentLanguage;
        }

        private void AddContentToIndex(IEnumerable<IContent> contents)
        {
            foreach (var content in contents)
            {
                // Add the content to the index
                _vulcanHandler.IndexContentEveryLanguage(content.ContentLink);

                // Recursively search for nested children and add them too.
                var descendants = _contentRepository.GetChildren<IContent>(content.ContentLink)?.ToList();

                if (descendants?.Any() == true)
                {
                    AddContentToIndex(descendants);
                }
            }
        }

        private void RemoveContentFromIndex(IEnumerable<IContent> contents)
        {
            foreach (var content in contents)
            {
                // Remove the content from the index
                _vulcanHandler.DeleteContentEveryLanguage(content.ContentLink, content.GetTypeName());

                // Recursively search for nested children and remove them too.
                var descendants = _contentRepository.GetChildren<IContent>(content.ContentLink)?.ToList();

                if (descendants?.Any() == true)
                {
                    RemoveContentFromIndex(descendants);
                }
            }
        }

        private void Service_DeletedContent(object sender, DeleteContentEventArgs e)
        {
            _vulcanHandler.DeleteContentEveryLanguage(e.ContentLink, e.Content.GetTypeName());
        }

        private void Service_DeletedContentLanguage(object sender, ContentEventArgs e)
        {
            _vulcanHandler.DeleteContentByLanguage(e.Content);
        }

        private void Service_MovedContent(object sender, ContentEventArgs e)
        {
            var descendants = _contentRepository.GetChildren<IContent>(e.ContentLink);

            if (e.TargetLink.CompareToIgnoreWorkID(SiteDefinition.Current.WasteBasket))
            {
                _vulcanHandler.DeleteContentEveryLanguage(e.ContentLink, e.Content.GetTypeName());

                RemoveContentFromIndex(descendants);
            }
            else
            {
                _vulcanHandler.IndexContentEveryLanguage(e.Content);

                AddContentToIndex(descendants);
            }
        }

        private void Service_PublishedContent(object sender, ContentEventArgs e)
        {
            // Update the index for the currently published content
            _vulcanHandler.IndexContentByLanguage(e.Content);

            // See if there are references to the content and if so, update the index for them as well
            var references = _contentRepository.GetReferencesToContent(e.ContentLink, false);

            foreach (var r in references.Where(x => !x.OwnerID.CompareToIgnoreWorkID(e.ContentLink)))
            {
                _vulcanHandler.IndexContentByLanguage(_contentRepository.Get<IContent>(r.OwnerID));
            }
        }
    }
}