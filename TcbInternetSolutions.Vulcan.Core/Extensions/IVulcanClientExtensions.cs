namespace TcbInternetSolutions.Vulcan.Core.Extensions
{

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;
    using EPiServer.Web.Routing;
    using Nest;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TcbInternetSolutions.Vulcan.Core;
    using TcbInternetSolutions.Vulcan.Core.Implementation;

    public static class IVulcanClientExtensions
    {
        private const string contentLinkField = "contentLink";
        private const string searchDescriptionField = "vulcanSearchDescription";
        private static readonly UrlResolver urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();

        /// <summary>
        /// Default search hit, which utilizes a 'vulcanSearchDescription' to set the summary, which can be added to content models via IVulcanSearchHitDescription; 
        /// </summary>
        /// <param name="contentHit"></param>
        /// <param name="contentLoader"></param>
        /// <returns></returns>
        public static VulcanSearchHit DefaultBuildSearchHit(IHit<IContent> contentHit, IContentLoader contentLoader)
        {
            ContentReference contentReference = null;

            if (ContentReference.TryParse(contentHit.Id, out contentReference))
            {                
                IContent content;

                if (contentLoader.TryGet(contentReference, out content))
                {
                    var searchDescriptionCheck = contentHit.Fields.Where(x => x.Key == searchDescriptionField).FirstOrDefault();
                    string storedDescription = searchDescriptionCheck.Value != null ? (searchDescriptionCheck.Value as JArray).FirstOrDefault().ToString() : null;
                    var fallbackDescription = content as IVulcanSearchHitDescription;
                    string description = storedDescription != null ? storedDescription.ToString() :
                            fallbackDescription != null ? fallbackDescription.VulcanSearchDescription : string.Empty;

                    var result = new VulcanSearchHit()
                    {
                        Id = content.ContentLink,
                        Title = content.Name,
                        Summary = description,
                        Url = urlResolver.GetUrl(contentReference)
                    };

                    return result;
                }
            }

            throw new Exception($"{nameof(contentHit)} doesn't implement IContent!");
        }

        /// <summary>
        /// Provides quick search, filtered by current user
        /// </summary>
        /// <param name="client"></param>
        /// <param name="query">Full text query against analyzed fields and uploaded assets if attachments are indexed.</param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchRoots"></param>
        /// <param name="includeTypes"></param>
        /// <param name="excludeTypes"></param>
        /// <param name="buildSearchHit">Can be used to customize how VulcanSearchHit is populated. Default is IVulcanClientExtensions.DefaultBuildSearchHit</param>
        /// <returns></returns>
        public static VulcanSearchHitList GetSearchHits(this IVulcanClient client,
                        string searchText,
                        int page,
                        int pageSize,
                        IEnumerable<ContentReference> searchRoots = null,
                        IEnumerable<Type> includeTypes = null,
                        IEnumerable<Type> excludeTypes = null,
                        Func<IHit<IContent>, IContentLoader, VulcanSearchHit> buildSearchHit = null
            )
        {
            var searchTextQuery = new QueryContainerDescriptor<IContent>().SimpleQueryString(sqs => sqs
                .Fields(f => f
                            .Field("*.analyzed")
                            .Field($"{VulcanFieldConstants.MediaContents}.content")
                            .Field($"{VulcanFieldConstants.MediaContents}.content_type"))
                .Query(searchText)
                .Analyzer("default")
            );

            return GetSearchHits(client, searchTextQuery, page, pageSize, searchRoots, includeTypes, excludeTypes, buildSearchHit);
        }

        /// <summary>
        /// Provides quick search, filtered by current user
        /// </summary>
        /// <param name="client"></param>
        /// <param name="query">Nest Query Container</param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchRoots"></param>
        /// <param name="includeTypes"></param>
        /// <param name="excludeTypes"></param>
        /// <param name="buildSearchHit">Can be used to customize how VulcanSearchHit is populated. Default is IVulcanClientExtensions.DefaultBuildSearchHit</param>
        /// <returns></returns>
        public static VulcanSearchHitList GetSearchHits(this IVulcanClient client,
                QueryContainer query,
                int page,
                int pageSize,
                IEnumerable<ContentReference> searchRoots = null,
                IEnumerable<Type> includeTypes = null,
                IEnumerable<Type> excludeTypes = null,
                Func<IHit<IContent>, IContentLoader, VulcanSearchHit> buildSearchHit = null
            )
        {
            if (includeTypes == null)
            {
                var pageTypes = typeof(PageData).GetSearchTypesFor((x => x.IsClass && !x.IsAbstract));
                var mediaTypes = typeof(MediaData).GetSearchTypesFor((x => x.IsClass && !x.IsAbstract));

                includeTypes = pageTypes.Union(mediaTypes);
            }

            buildSearchHit = buildSearchHit ?? DefaultBuildSearchHit;
            pageSize = pageSize < 1 ? 10 : pageSize;
            page = page < 1 ? 1 : page;
            var searchForTypes = includeTypes.Except(excludeTypes ?? new Type[] { });
            var hits = client.SearchContent<VulcanContentHit>(d => d
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Fields(fs => fs.Field(searchDescriptionField).Field(p => p.ContentLink)) // only return contentLink
                    .Query(q => query)
                    //.Highlight(h => h.Encoder("html").Fields(f => f.Field("*")))
                    .Aggregations(agg => agg.Terms("types", t => t.Field("_type"))),
                    includeNeutralLanguage: true,
                    typeFilter: searchForTypes,
                    principleReadFilter: PrincipalInfo.Current.Principal,
                    rootReferences: searchRoots
            );

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var searchHits = hits.Hits.Select(x => buildSearchHit(x, contentLoader));
            var results = new VulcanSearchHitList(searchHits) { TotalHits = hits.Total, ResponseContext = hits, Page = page, PageSize = pageSize };

            return results;
        }        
    }
}
