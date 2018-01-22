using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    [ServiceConfiguration(ServiceType = typeof(IGoogleProductFeedService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class GoogleProductFeedService : IGoogleProductFeedService
    {
        private Dictionary<Type, IGoogleProductFeed> Feeds { get; } = new Dictionary<Type, IGoogleProductFeed>();

        public IGoogleProductFeed<T> CreateFeed<T>(string urlSegment) where T : VariationContent
        {
            return CreateFeed<GoogleProductFeed<T>, T>(urlSegment); // use default concrete implementation
        }

        public IGoogleProductFeed<TVariationContent> CreateFeed<TGoogleProductFeed, TVariationContent>(string urlSegment) where TGoogleProductFeed : IGoogleProductFeed<TVariationContent>, new() where TVariationContent : VariationContent
        {
            if(string.IsNullOrWhiteSpace(urlSegment))
            {
                throw new ArgumentException("Url segment must be set to add a feed");
            }

            if (Feeds.ContainsKey(typeof(TVariationContent)))
            {
                if(Feeds[typeof(TVariationContent)].UrlSegment != urlSegment)
                {
                    throw new ArgumentException("That feed was already added for type " + typeof(TVariationContent).FullName + " with a different url segment");
                }

                return Feeds[typeof(TVariationContent)] as IGoogleProductFeed<TVariationContent>;
            }

            var url = urlSegment;

            if (!url.StartsWith("/")) url = "/" + url;
            if (!url.EndsWith("/")) url += "/";

            url = "GoogleProductFeed" + url + "{market}/{language}/{currency}";

            var route = RouteTable.Routes.MapRoute(
                "GoogleProductFeed " + typeof(TVariationContent).FullName,
                url,
                new { controller = "GoogleProductFeed", action = "Feed", type = typeof(TVariationContent), market = "", language = "", currency = "" },
                new[] { "TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed" });

            var feed = new TGoogleProductFeed()
            {
                Route = route,
                UrlSegment = urlSegment
            };

            Feeds.Add(typeof(TVariationContent), feed);

            return feed;
        }

        public IGoogleProductFeed<T> GetFeed<T>() where T : VariationContent
        {
            return Feeds.ContainsKey(typeof(T)) ? Feeds[typeof(T)] as IGoogleProductFeed<T> : null;
        }

        public IGoogleProductFeed GetFeed(Type type)
        {
            return Feeds.ContainsKey(type) ? Feeds[type]  : null;
        }
    }
}
