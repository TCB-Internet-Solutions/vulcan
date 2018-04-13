using System;
using System.Linq;
using EPiServer.ServiceLocation;
using EPiServer.Web.Hosting;

namespace TcbInternetSolutions.Vulcan.UI.Support
{
    public static class Helper
    {
        public static string ResolveView(string view)
        {
            var protectedModulesVpp = ServiceLocator.Current.
                GetInstance<VirtualPathRegistrationHandler>()
                    .RegisteredVirtualPathProviders.Where(p => p.Key is VirtualPathNonUnifiedProvider && ((VirtualPathNonUnifiedProvider) p.Key).ProviderName == "ProtectedModules")
                    .ToList();

            if (!protectedModulesVpp.Any() ||!(protectedModulesVpp[0].Key is VirtualPathNonUnifiedProvider provider))
                throw new Exception("Cannot resolve the Vulcan UI views");

            var path = provider.ConfigurationParameters["physicalPath"].Replace(@"\", "/");

            if (!path.StartsWith("~"))
            {
                if (!path.StartsWith("/"))
                {
                    path = "~/" + path;
                }
                else
                {
                    path = "~" + path;
                }
            }
            else if (!path.StartsWith("/"))
            {
                if (!path.StartsWith("~"))
                {
                    path = "~/" + path;
                }
            }

            if (!path.EndsWith("/")) path += "/";

            if (!view.StartsWith("/")) view = "/" + view;

            return path + "TcbInternetSolutions.Vulcan.UI/Views" + view;
        }
    }
}
