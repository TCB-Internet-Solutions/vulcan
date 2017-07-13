using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
