using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Setup events for content syncs to search
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class VulcanIndexSynchronization : IInitializableModule
    {
        Injected<IContentEvents> ContentEvents;

        Injected<IVulcanHandler> VulcanHandler;

        Injected<IContentRepository> ContentRepository;

        /// <summary>
        /// Init event
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent += Service_PublishedContent;
            ContentEvents.Service.MovedContent += Service_MovedContent;
            ContentEvents.Service.DeletedContent += Service_DeletedContent;
            ContentEvents.Service.DeletedContentLanguage += Service_DeletedContentLanguage;
        }

        void Service_DeletedContentLanguage(object sender, EPiServer.ContentEventArgs e)
        {
            VulcanHandler.Service.DeleteContentByLanguage(e.Content);
        }

        void Service_DeletedContent(object sender, EPiServer.DeleteContentEventArgs e)
        {
            VulcanHandler.Service.DeleteContentEveryLanguage(e.ContentLink);
        }

        void Service_MovedContent(object sender, EPiServer.ContentEventArgs e)
        {
            if (e.TargetLink.CompareToIgnoreWorkID(SiteDefinition.Current.WasteBasket))
            {
                VulcanHandler.Service.DeleteContentEveryLanguage(e.ContentLink);
            }
            else
            {
                VulcanHandler.Service.IndexContentEveryLanguage(e.Content);
            }
        }

        void Service_PublishedContent(object sender, EPiServer.ContentEventArgs e)
        {
            // Update the index for the currently published content
            VulcanHandler.Service.IndexContentByLanguage(e.Content);

            // See if there are references to the content and if so, update the index for them as well
            var references = ContentRepository.Service.GetReferencesToContent(e.ContentLink, false);

            foreach (var r in references.Where(x => !x.OwnerID.CompareToIgnoreWorkID(e.ContentLink)))
            {
                VulcanHandler.Service.IndexContentByLanguage(ContentRepository.Service.Get<IContent>(r.OwnerID));
            }

        }

        /// <summary>
        /// Un-init event
        /// </summary>
        /// <param name="context"></param>
        public void Uninitialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent -= Service_PublishedContent;
            ContentEvents.Service.MovedContent -= Service_MovedContent;
            ContentEvents.Service.DeletedContent -= Service_DeletedContent;
            ContentEvents.Service.DeletedContentLanguage -= Service_DeletedContentLanguage;
        }
    }
}