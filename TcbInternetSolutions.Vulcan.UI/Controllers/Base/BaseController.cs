using EPiServer.ServiceLocation;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Controllers.Base
{
    public abstract class BaseController : Controller
    {
        public Injected<IVulcanHandler> VulcanHandler { get; set; }
    }
}
