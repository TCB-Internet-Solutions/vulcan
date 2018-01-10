using EPiServer.Shell.Navigation;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.UI.Models.ViewModels;
using TcbInternetSolutions.Vulcan.UI.Support;
using TcbInternetSolutions.Vulcan.Core.Extensions;
using System.Linq;
using System;

namespace TcbInternetSolutions.Vulcan.UI.Controllers
{
    [Authorize(Roles = "Administrators,CmsAdmins,WebAdmins,VulcanAdmins")]
    public class HomeController : Base.BaseController
    {
        public HomeController(IVulcanHandler vulcanHandler) : base(vulcanHandler) { }

        [MenuItem("/global/vulcan", Text = "Vulcan")]
        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel()
            {
                VulcanHandler = VulcanHandler,
                PocoIndexers = typeof(IVulcanPocoIndexer)
                    .GetSearchTypesFor(VulcanFieldConstants.DefaultFilter).Select(x => Activator.CreateInstance(x) as IVulcanPocoIndexer)
            };

            return View(Helper.ResolveView("Home/Index.cshtml"), viewModel);
        }
    }
}
