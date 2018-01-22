namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    using Core;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using EPiServer.Web.Routing;
    using Implementation;
    using Nest;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using static VulcanFieldConstants;

    /// <summary>
    /// Vulcan client extensions
    /// </summary>
    public static class VulcanClientExtensions
    {
        /// <summary>
        /// IUrlResolver dependency
        /// </summary>
        public static Injected<IUrlResolver> UrlResolver { get; set; }

        /// <summary>
        /// IVulcanHandler dependency
        /// </summary>
        public static Injected<IVulcanHandler> VulcanHandler { get; set; }

        /// <summary>
        /// Gets a list of Vulcan customizers
        /// </summary>
        public static IEnumerable<IVulcanCustomizer> Customizers => ServiceLocator.Current.GetAllInstances<IVulcanCustomizer>();

        /// <summary>
        /// Allows for customizations on analyzers and mappings.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logger"></param>
        public static void RunCustomizers(this IVulcanClient client, ILogger logger)
        {
            var customizers = Customizers;

            // run index updaters first, incase they are creating analyzers the mapping need
            foreach (var customizer in customizers)
            {
                try
                {
                    var updateResponse = customizer?.CustomIndexUpdater?.Invoke(client);

                    if (updateResponse?.IsValid == false)
                    {
                        logger.Error("Could not update index " + client.IndexName + ": " + updateResponse.DebugInformation);
                    }
                }
                catch (NotImplementedException) { }
            }

            // then run the mappings
            foreach (var customizer in customizers)
            {
                try
                {
                    var mappingResponse = customizer?.CustomMapper?.Invoke(client);

                    if (mappingResponse?.IsValid == false)
                    {
                        logger.Error("Could not add mapping for index " + client.IndexName + ": " + mappingResponse.DebugInformation);
                    }
                }
                catch (NotImplementedException) { }
            }
        }

        /// <summary>
        /// Allows for creation/updates of index templates
        /// </summary>
        /// <param name="client"></param>
        /// <param name="indexPrefix"></param>
        /// <param name="logger"></param>
        public static void RunCustomIndexTemplates(this IVulcanClient client, string indexPrefix, ILogger logger)
        {
            foreach (var customizer in Customizers)
            {
                try
                {
                    var updateIndexTemplate = customizer?.CustomIndexTemplate?.Invoke(client, indexPrefix);

                    if (updateIndexTemplate?.IsValid == false)
                    {
                        logger.Error("Could not update index template " + client.IndexName + ": " + updateIndexTemplate.DebugInformation);
                    }
                }
                catch (NotImplementedException) { }
            }
        }

        /// <summary>
        /// Adds full name as search type, and ensures invariant culture for POCO searching.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="searchDescriptor"></param>
        /// <returns></returns>
        public static ISearchResponse<T> PocoSearch<T>(this IVulcanClient client, Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null) where T : class
        {
            var tempClient = client.Language.Equals(CultureInfo.InvariantCulture) ? client : VulcanHandler.Service.GetClient(CultureInfo.InvariantCulture);
            var resolvedDescriptor = searchDescriptor?.Invoke(new SearchDescriptor<T>()) ?? new SearchDescriptor<T>();
            resolvedDescriptor = resolvedDescriptor.Type(typeof(T).FullName);

            return tempClient.Search<T>(resolvedDescriptor);
        }

        /// <summary>
        /// Default search hit, which utilizes a 'vulcanSearchDescription' to set the summary, which can be added to content models via IVulcanSearchHitDescription; 
        /// </summary>
        /// <param name="contentHit"></param>
        /// <param name="contentLoader"></param>
        /// <returns></returns>
        public static VulcanSearchHit DefaultBuildSearchHit(IHit<IContent> contentHit, IContentLoader contentLoader)
        {
            if (!ContentReference.TryParse(contentHit.Id, out var contentReference) || !contentLoader.TryGet(contentReference, out IContent content))
                throw new Exception($"{nameof(contentHit)} doesn't implement IContent!");

            var localizable = content as ILocalizable;
            var searchDescriptionCheck = contentHit.Fields.FirstOrDefault(x => x.Key == SearchDescriptionField);
            var storedDescription = (searchDescriptionCheck.Value as JArray)?.FirstOrDefault()?.ToString();
            var description = storedDescription ?? (content as IVulcanSearchHitDescription)?.VulcanSearchDescription ?? string.Empty;

            var result = new VulcanSearchHit()
            {
                Id = content.ContentLink,
                Title = content.Name,
                Summary = description,
                Url = UrlResolver.Service.GetUrl(contentReference, localizable?.Language.Name ?? "", new UrlResolverArguments()) // fixes null ref exception caused by extension
            };

            return result;
        }

        /// <summary>
        /// Provides quick search, filtered by current user
        /// </summary>
        /// <param name="client"></param>
        /// <param name="searchText">Full text query against analyzed fields and uploaded assets if attachments are indexed.</param>
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
            QueryContainer searchTextQuery = new QueryContainerDescriptor<IContent>();

            // only add query string if query has value
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchTextQuery = new QueryContainerDescriptor<IContent>().SimpleQueryString(sqs => sqs
                    .Fields(f => f
                                .AllAnalyzed()
                                .Field($"{MediaContents}.content")
                                .Field($"{MediaContents}.content_type"))
                    .Query(searchText)
                );
            }

            searchTextQuery = searchTextQuery.FilterForPublished<IContent>();

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

            // restrict to start page and global blocks if not otherwise specified
            if (searchRoots == null && !ContentReference.IsNullOrEmpty(ContentReference.StartPage))
                searchRoots = new[] { ContentReference.StartPage, ContentReference.GlobalBlockFolder };

            buildSearchHit = buildSearchHit ?? DefaultBuildSearchHit;
            pageSize = pageSize < 1 ? 10 : pageSize;
            page = page < 1 ? 1 : page;
            var searchForTypes = includeTypes.Except(excludeTypes ?? new Type[] { });
            var hits = client.SearchContent<VulcanContentHit>(d => d
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .FielddataFields(fs => fs.Field(SearchDescriptionField).Field(p => p.ContentLink)) // only return contentLink
                    .Query(q => query)
                    //.Highlight(h => h.Encoder("html").Fields(f => f.Field("*")))
                    .Aggregations(agg => agg.Terms("types", t => t.Field(TypeField))),
                    typeFilter: searchForTypes,
                    principleReadFilter: UserExtensions.GetUser(),
                    rootReferences: searchRoots,
                    includeNeutralLanguage: true
            );

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var searchHits = hits.Hits.Select(x => buildSearchHit(x, contentLoader));
            var results = new VulcanSearchHitList(searchHits) { TotalHits = hits.Total, ResponseContext = hits, Page = page, PageSize = pageSize };

            return results;
        }
    }
}
