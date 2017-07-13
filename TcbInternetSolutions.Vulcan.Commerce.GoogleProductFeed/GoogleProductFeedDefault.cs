using System;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using System.Web.Routing;
using System.Web.Mvc;
using EPiServer.ServiceLocation;
using EPiServer.Commerce.Catalog.ContentTypes;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class GoogleProductFeedDefault : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            ServiceLocator.Current.GetInstance<IGoogleProductFeedService>().CreateFeed<VariationContent>("Default");
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}