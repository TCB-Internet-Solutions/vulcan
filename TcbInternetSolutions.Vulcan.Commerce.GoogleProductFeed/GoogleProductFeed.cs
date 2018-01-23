using EPiServer.Commerce.Catalog;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Markets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Routing;
using TcbInternetSolutions.Vulcan.Commerce.Extensions;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.Core.Extensions;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    public class GoogleProductFeed<T> : IGoogleProductFeed<T> where T : VariationContent
    {
        public Injected<AssetUrlResolver> AssetUrlResolver { get; set; }

        public Injected<IMarketService> MarketService { get; set; }

        public Injected<ICurrentMarket> CurrentMarket { get; set; }

        public string UrlSegment { get; set; }

        public Route Route { get; set; }

        public Func<Nest.SearchDescriptor<T>, Nest.SearchDescriptor<T>> Query { get; set; } = q => q.Size(9999);

        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public Func<T, string> DescriptionSelector { get; set; }

        public Func<T, string> GTINSelector { get; set; }

        public Func<T, string> AvailabilitySelector { get; set; }

        public Func<T, string> GoogleProductCategorySelector { get; set; }

        public Func<T, string> BrandSelector { get; set; }

        public Func<T, string> MPNSelector { get; set; }

        public Func<T, string> ShippingSelector { get; set; }

        public Func<T, string> TaxSelector { get; set; }

        public Func<T, string> ConditionSelector { get; set; }

        public Func<T, string> AdultSelector { get; set; }

        public IList<GoogleProductFeedEntry> GetEntries(string market, string language, string currency)
        {
            var selectedMarket = string.IsNullOrWhiteSpace(market) ?
                CurrentMarket.Service.GetCurrentMarket() : 
                MarketService.Service.GetAllMarkets()?.FirstOrDefault(m => m.MarketId.Value.Equals(market, StringComparison.InvariantCultureIgnoreCase));

            if(selectedMarket == null)
            {
                throw new ArgumentException("Selected market could not be found (or default could not be found if no market specified)");
            }

            market = selectedMarket.MarketId.Value; // ensure correct casing

            if (string.IsNullOrWhiteSpace(language))
            {
                language = selectedMarket.DefaultLanguage.Name;
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                currency = selectedMarket.DefaultCurrency.CurrencyCode;
            }

            currency = currency.ToUpper();

            var entries = new Dictionary<string, GoogleProductFeedEntry>();

            var client = VulcanHandler.Service.GetClient(new CultureInfo(language));

            var hits = client?.SearchContent(Query, false, new[] { ReferenceConverter.Service.GetRootLink() })?.GetHitContents();

            if (hits?.Any() == true)
            {
                foreach (var hit in hits)
                {
                    var product = hit.Value as T;

                    if (entries.ContainsKey(product.Code)) continue;

                    var price = hit.Key.Source.GetPrice(market, currency);

                    if (price == 0) continue;

                    var image = AssetUrlResolver.Service.GetAssetUrl(product);

                    if (string.IsNullOrWhiteSpace(image)) continue;

                    var description = DescriptionSelector == null
                        ? product.DisplayName
                        : DescriptionSelector.Invoke(product);
                    if (string.IsNullOrWhiteSpace(description))
                        description = product.DisplayName; // double-check in case it's empty

                    entries.Add(product.Code, new GoogleProductFeedEntry
                    {
                        Id = product.Code?.Replace('\t', ' '),
                        Title = product.DisplayName?.Replace('\t', ' '),
                        Description = description?.Replace('\t', ' '),
                        Availability =
                            AvailabilitySelector == null
                                ? "in stock"
                                : AvailabilitySelector.Invoke(product)?.Replace('\t', ' '),
                        Adult = AdultSelector == null ? "no" : AdultSelector.Invoke(product)?.Replace('\t', ' '),
                        Brand = BrandSelector?.Invoke(product)?.Replace('\t', ' '),
                        Condition = ConditionSelector == null
                            ? "new"
                            : ConditionSelector.Invoke(product)?.Replace('\t', ' '),
                        GoogleProductCategory = GoogleProductCategorySelector?.Invoke(product)?.Replace('\t', ' '),
                        GTIN = GTINSelector?.Invoke(product)?.Replace('\t', ' '),
                        MPN = MPNSelector?.Invoke(product)?.Replace('\t', ' '),
                        Shipping = ShippingSelector?.Invoke(product)?.Replace('\t', ' '),
                        Tax = TaxSelector?.Invoke(product)?.Replace('\t', ' '),
                        ImageLink = FullUrl(image),
                        Link = FullUrl(UrlResolver.Current.GetUrl(product)),
                        Price = price.ToString("F2", CultureInfo.InvariantCulture) + " " + currency.ToUpper()
                    });
                }
            }

            return entries.Values.ToList();
        }

        private static string FullUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // code from https://stackoverflow.com/questions/29897769/episerver-get-absolute-friendly-url-for-given-culture-for-page

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            return uri.IsAbsoluteUri ? url : new Uri(HttpContext.Current.Request.Url, uri).ToString();
        }
    }
}
