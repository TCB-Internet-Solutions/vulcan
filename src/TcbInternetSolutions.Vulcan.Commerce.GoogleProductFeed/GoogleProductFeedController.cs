using EPiServer.ServiceLocation;
using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    public class GoogleProductFeedController : Controller
    {
        public Injected<IGoogleProductFeedService> GoogleProductFeedService { get; set; }

        public ActionResult Feed(Type type, string market, string language, string currency, bool json = false)
        {
            var entries = GoogleProductFeedService.Service.GetFeed(type)?.GetEntries(market, language, currency);

            if (json)
            {
                return Json(entries, JsonRequestBehavior.AllowGet); // for debugging
            }

            const string format = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}";

            var text = string.Format(format, "id", "title", "description", "link", "image_link", "availability", "price", "google_product_category", "brand", "gtin", "mpn", "identifier_exists", "condition", "adult", "shipping", "tax") + Environment.NewLine;
            
            if (entries != null && entries.Any())
            {
                foreach(var entry in entries)
                {
                    text += string.Format(format, 
                        entry.Id,
                        entry.Title,
                        entry.Description,
                        entry.Link,
                        entry.ImageLink,
                        entry.Availability,
                        entry.Price,
                        entry.GoogleProductCategory,
                        entry.Brand,
                        entry.GTIN,
                        entry.MPN,
                        entry.IdentifierExists  ? "yes" : "no",
                        entry.Condition,
                        entry.Adult,
                        entry.Shipping,
                        entry.Tax
                        ) + Environment.NewLine;
                }
            }

            Response.Charset = "utf-8";

            return File(Encoding.UTF8.GetBytes(text), "text");
        }

    }
}
