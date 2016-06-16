using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Controllers.Base
{
    public abstract class BaseController : Controller
    {
        public Injected<IVulcanHandler> VulcanHandler { get; set; }
    }
}
