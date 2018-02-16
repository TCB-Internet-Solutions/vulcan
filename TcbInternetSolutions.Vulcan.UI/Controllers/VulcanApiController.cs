using System;
using System.Globalization;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Controllers
{
    [Authorize(Roles="Administrators, WebAdmins, CmsAdmins, VulcanAdmins")]
    public class VulcanApiController : Base.BaseController
    {
        public VulcanApiController(IVulcanHandler vulcanHandler) : base(vulcanHandler) { }

        [HttpGet]
        public ActionResult ListSynonyms(string language)
        {
            var client = VulcanHandler.GetClient(string.IsNullOrWhiteSpace(language) ? CultureInfo.InvariantCulture : new CultureInfo(language));

            return Json(client.GetSynonyms(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddSynonym(string language, string term, string synonyms, bool biDirectional)
        {
            if(string.IsNullOrWhiteSpace(term))
            {
                throw new Exception($"AddSynonym: {nameof(term)} cannot be blank");
            }
            
            if(string.IsNullOrWhiteSpace(synonyms))
            {
                throw new Exception($"AddSynonym: {nameof(synonyms)} cannot be blank");
            }

            var client = VulcanHandler.GetClient(string.IsNullOrWhiteSpace(language) ? CultureInfo.InvariantCulture : new CultureInfo(language));
            client.AddSynonym(term, synonyms.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries), biDirectional);

            return Json("OK");
        }

        [HttpPost]
        public ActionResult RemoveSynonym(string language, string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                throw new Exception($"RemoveSynonym: {nameof(term)} cannot be blank");
            }

            var client = VulcanHandler.GetClient(string.IsNullOrWhiteSpace(language) ? CultureInfo.InvariantCulture : new CultureInfo(language));
            client.RemoveSynonym(term);

            return Json("OK");
        }
    }
}
