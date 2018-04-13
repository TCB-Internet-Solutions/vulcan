using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using System.Web.Mvc;
using System.Web.Routing;

namespace TcbInternetSolutions.Vulcan.UI.Support
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class RegisterRoutes : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            RouteTable.Routes.MapRoute(
                "ListSynonyms",
                "Vulcan-Api/Synonym/List/{Language}",
                new { controller = "VulcanApi", action = "ListSynonyms", Language = "" });

            RouteTable.Routes.MapRoute(
                "AddSynonym",
                "Vulcan-Api/Synonym/Add/{Term}/{Synonyms}/{BiDirectional}/{Language}",
                new { controller = "VulcanApi", action = "AddSynonym", Language = "" });

            RouteTable.Routes.MapRoute(
                "RemoveSynonym",
                "Vulcan-Api/Synonym/Remove/{Term}/{Language}",
                new { controller = "VulcanApi", action = "RemoveSynonym", Language = "" });
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}