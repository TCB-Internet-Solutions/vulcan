using EPiServer.Commerce.Catalog.ContentTypes;
using System;
using System.Collections.Generic;
using System.Web.Routing;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    public interface IGoogleProductFeed
    {
        string UrlSegment { get; set; }

        Route Route { get; set; }

        IList<GoogleProductFeedEntry> GetEntries(string market, string language, string currency);
    }

    public interface IGoogleProductFeed<T> : IGoogleProductFeed where T : VariationContent
    {
        Func<Nest.SearchDescriptor<T>, Nest.SearchDescriptor<T>> Query { get; set; }

        Func<T, string> DescriptionSelector { get; set; }

        Func<T, string> AvailabilitySelector { get; set; }

        Func<T, string> GoogleProductCategorySelector { get; set; }

        Func<T, string> BrandSelector { get; set; }

        Func<T, string> GTINSelector { get; set; }

        Func<T, string> MPNSelector { get; set; }

        Func<T, string> ShippingSelector { get; set; }

        Func<T, string> TaxSelector { get; set; }

        Func<T, string> ConditionSelector { get; set; }

        Func<T, string> AdultSelector { get; set; }
    }
}
