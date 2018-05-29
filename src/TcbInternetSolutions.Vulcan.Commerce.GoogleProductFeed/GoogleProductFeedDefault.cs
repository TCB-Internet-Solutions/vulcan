using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class GoogleProductFeedDefault : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            context.Locate.Advanced.GetInstance<IGoogleProductFeedService>().CreateFeed<VariationContent>("Default");
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}