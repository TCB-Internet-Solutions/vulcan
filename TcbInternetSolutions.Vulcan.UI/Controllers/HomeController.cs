using EPiServer.Shell.Navigation;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.UI.Models.ViewModels;
using TcbInternetSolutions.Vulcan.UI.Support;

namespace TcbInternetSolutions.Vulcan.UI.Controllers
{
    [Authorize(Roles = "Administrators,CmsAdmins,WebAdmins,VulcanAdmins")]
    public class HomeController : Base.BaseController
    {
        [MenuItem("/global/vulcan", Text = "Vulcan")]
        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel()
            {
                VulcanHandler = VulcanHandler.Service
            };

            return View(Helper.ResolveView("Home/Index.cshtml"), viewModel);
        }
    }
}
