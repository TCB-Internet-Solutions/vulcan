using System;
using System.Linq;
using EPiServer.Web.Hosting;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.UI.Support
{
    public static class Helper
    {
        public static string ResolveView(string view, VirtualPathRegistrationHandler vppHandler = null)
        {
            var resolvedVppHandler = vppHandler ?? VulcanHelper.GetService<VirtualPathRegistrationHandler>();
            var protectedModulesVpp = resolvedVppHandler
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
