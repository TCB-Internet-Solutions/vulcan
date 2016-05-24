using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace TcbInternetSolutions.Vulcan.UI.Controllers
{
    [Authorize(Roles="Administrators, WebAdmins, CmsAdmins, VulcanAdmins")]
    public class VulcanApiController : Base.BaseController
    {
        [HttpGet]
        public ActionResult ListSynonyms(string Language)
        {
            var client = VulcanHandler.Service.GetClient(string.IsNullOrWhiteSpace(Language) ? CultureInfo.InvariantCulture : new CultureInfo(Language));

            return Json(client.GetSynonyms(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddSynonym(string Language, string Term, string Synonyms, bool BiDirectional)
        {
            if(string.IsNullOrWhiteSpace(Term))
            {
                throw new Exception("AddSynonym: Term cannot be blank");
            }
            
            if(string.IsNullOrWhiteSpace(Synonyms))
            {
                throw new Exception("AddSynonym: Synonyms cannot be blank");
            }

            var client = VulcanHandler.Service.GetClient(string.IsNullOrWhiteSpace(Language) ? CultureInfo.InvariantCulture : new CultureInfo(Language));

            var SynonymsArray = Synonyms.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            client.AddSynonym(Term, Synonyms.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries), BiDirectional);

            return Json("OK");
        }

        [HttpPost]
        public ActionResult RemoveSynonym(string Language, string Term)
        {
            if (string.IsNullOrWhiteSpace(Term))
            {
                throw new Exception("RemoveSynonym: Term cannot be blank");
            }

            var client = VulcanHandler.Service.GetClient(string.IsNullOrWhiteSpace(Language) ? CultureInfo.InvariantCulture : new CultureInfo(Language));

            client.RemoveSynonym(Term);

            return Json("OK");
        }
    }
}
