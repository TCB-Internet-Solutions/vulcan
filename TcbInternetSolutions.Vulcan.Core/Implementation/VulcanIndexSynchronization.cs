using System;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Core;
using EPiServer.Web;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class VulcanIndexSynchronization : IInitializableModule
    {
        public Injected<IContentEvents> ContentEvents { get; set; }
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public void Initialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent += Service_PublishedContent;
            ContentEvents.Service.MovedContent += Service_MovedContent;
            ContentEvents.Service.DeletedContent += Service_DeletedContent;
        }

        void Service_DeletedContent(object sender, EPiServer.DeleteContentEventArgs e)
        {
            VulcanHandler.Service.Client.DeleteContent(e.Content);
        }

        void Service_MovedContent(object sender, EPiServer.ContentEventArgs e)
        {
            if (e.TargetLink.CompareToIgnoreWorkID(SiteDefinition.Current.WasteBasket))
            {
                VulcanHandler.Service.Client.DeleteContent(e.Content);
            }
            else
            {
                VulcanHandler.Service.Client.IndexContent(e.Content);
            }
        }

        void Service_PublishedContent(object sender, EPiServer.ContentEventArgs e)
        {
            VulcanHandler.Service.Client.IndexContent(e.Content);
        }

        public void Uninitialize(InitializationEngine context)
        {
            ContentEvents.Service.PublishedContent -= Service_PublishedContent;
            ContentEvents.Service.MovedContent -= Service_MovedContent;
            ContentEvents.Service.DeletedContent -= Service_DeletedContent;
        }
    }
}