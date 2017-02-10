using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web;

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
            VulcanHandler.Service.IndexContentByLanguage(e.Content);
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