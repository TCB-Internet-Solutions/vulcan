using EPiServer.Commerce.Catalog.ContentTypes;
using System;

namespace TcbInternetSolutions.Vulcan.Commerce.GoogleProductFeed
{
    public interface IGoogleProductFeedService
    {
        IGoogleProductFeed<T> CreateFeed<T>(string urlSegment) where T : VariationContent;

        IGoogleProductFeed<TVariationContent> CreateFeed<TGoogleProductFeed, TVariationContent>(string urlSegment) where TGoogleProductFeed : IGoogleProductFeed<TVariationContent>, new() where TVariationContent : VariationContent;

        IGoogleProductFeed<T> GetFeed<T>() where T : VariationContent;

        IGoogleProductFeed GetFeed(Type type);
    }
}
