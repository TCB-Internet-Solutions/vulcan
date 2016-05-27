using EPiServer.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.UI.Support
{
    public static class Helper
    {
        public static string ResolveView(string View)
        {
            var protectedModulesVpp = EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<EPiServer.Web.Hosting.VirtualPathRegistrationHandler>().RegisteredVirtualPathProviders.Where(p => (p.Key is EPiServer.Web.Hosting.VirtualPathNonUnifiedProvider) && ((p.Key as EPiServer.Web.Hosting.VirtualPathNonUnifiedProvider).ProviderName == "ProtectedModules"));

            if(protectedModulesVpp.Count() > 0)
            {
                var path = (protectedModulesVpp.First().Key as EPiServer.Web.Hosting.VirtualPathNonUnifiedProvider).ConfigurationParameters["physicalPath"].Replace(@"\", "/");

                if(!path.StartsWith("~"))
                {
                    if(!path.StartsWith("/"))
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

                if (!View.StartsWith("/")) View = "/" + View;

                return path + "TcbInternetSolutions.Vulcan.UI/Views" + View;
            }

            throw new Exception("Cannot resolve the Vulcan UI views");
        }
    }
}
