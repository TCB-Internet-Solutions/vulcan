using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

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