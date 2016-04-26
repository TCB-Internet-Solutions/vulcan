using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ServiceConfiguration(typeof(IVulcanHandler),Lifecycle=ServiceInstanceScope.Singleton)]
    public class VulcanHandler : IVulcanHandler
    {
        public IVulcanClient Client
        {
            get 
            {
                return VulcanHelper.GetClient();
            }
        }
    }
}
