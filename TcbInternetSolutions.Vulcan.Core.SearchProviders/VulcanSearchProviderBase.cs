namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{
    using Core.Extensions;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Core.Html;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework;
    using EPiServer.Framework.Localization;
    using EPiServer.Globalization;
    using EPiServer.Shell;
    using EPiServer.Shell.Search;
    using EPiServer.Shell.Web.Mvc.Html;
    using EPiServer.SpecializedProperties;
    using EPiServer.Web;
    using Extensions;
    using Implementation;
    using Nest;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// Base class for UI search providers
    /// </summary>
    /// <typeparam name="TContent"></typeparam>
    public abstract class VulcanSearchProviderBase<TContent> :
        ISearchProvider, ISortable where TContent : class, IContent
    {
        /// <summary>
        /// Link for the search hit, which should be a link to the edit page for the content.
        /// </summary>
        public Func<IContent, ContentReference, string, string> EditPath;

        /// <summary>
        /// Content repository
        /// </summary>
        protected IContentRepository ContentRepository;

        /// <summary>
        /// Content type repository
        /// </summary>
        protected IContentTypeRepository ContentTypeRepository;

        /// <summary>
        /// Site definition resolver
        /// </summary>
        protected ISiteDefinitionResolver SiteDefinitionResolver;

        /// <summary>
        /// Localization service
        /// </summary>
        protected LocalizationService LocalizationService;

        /// <summary>
        /// UI descriptor registry
        /// </summary>
        protected UIDescriptorRegistry UiDescriptorRegistry;

        /// <summary>
        /// Vulcan handler
        /// </summary>
        protected IVulcanHandler VulcanHandler;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        /// <param name="contentRepository"></param>
        /// <param name="contentTypeRepository"></param>
        /// <param name="localizationService"></param>
        /// <param name="uiDescriptorRegistry"></param>
        /// <param name="enterpriseSettings"></param>
        protected VulcanSearchProviderBase(IVulcanHandler vulcanHandler, IContentRepository contentRepository, IContentTypeRepository contentTypeRepository, LocalizationService localizationService, UIDescriptorRegistry uiDescriptorRegistry, ISiteDefinitionResolver enterpriseSettings)
        {
            VulcanHandler = vulcanHandler;
            ContentRepository = contentRepository;
            ContentTypeRepository = contentTypeRepository;
            LocalizationService = localizationService;
            UiDescriptorRegistry = uiDescriptorRegistry;
            SiteDefinitionResolver = enterpriseSettings;

            EditPath = (contentData, contentLink, languageName) =>
            {
                var uri = contentData.GetUri();

                return !string.IsNullOrWhiteSpace(languageName) ? $"{uri}#language={languageName}" : uri;
            };
        }

        /// <summary>
        /// UI search area
        /// </summary>
        public abstract string Area { get; }

        /// <summary>
        /// UI search category
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Sort order
        /// </summary>
        public virtual int SortOrder => 99;

        /// <summary>
        /// Include invariant results
        /// </summary>
        public virtual bool IncludeInvariant => false;

        /// <summary>
        /// The root path to the tool tip resource for the content search provider
        /// </summary>
        protected virtual string ToolTipResourceKeyBase => null;

        /// <summary>
        /// The tool tip key for the content type name.
        /// </summary>        
        protected virtual string ToolTipContentTypeNameResourceKey => null;

        /// <summary>
        /// Search
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public virtual IEnumerable<SearchResult> Search(Query query)
        {
            List<ContentReference> searchRoots = null;
            var searchText = query.SearchQuery;

            if (query.SearchRoots?.Any() == true)
            {
                searchRoots = new List<ContentReference>();

                foreach (var item in query.SearchRoots)
                {
                    if (ContentReference.TryParse(item, out var c))
                        searchRoots.Add(c);
                }
            }

            var typeRestriction = typeof(TContent).GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);

            // Special condition for BlockData since it doesn't derive from BlockData
            if (typeof(TContent) == typeof(VulcanContentHit))
            {
                typeRestriction = typeof(BlockData).GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);
            }

            var hits = new List<ISearchResponse<IContent>>();

            var clients = VulcanHandler.GetClients();

            if (clients != null)
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var client in clients)
                {
                    if (client.Language.Equals(CultureInfo.InvariantCulture) && !IncludeInvariant) continue;

                    var clientHits = client.SearchContent<IContent>(d => d
                            .Take(query.MaxResults)
                            .FielddataFields(fs => fs.Field(p => p.ContentLink)) // only return id for performance
                            .Query(x =>
                                x.SimpleQueryString(sqs =>
                                    sqs.Fields(f => f
                                            .AllAnalyzed()
                                            .Field($"{VulcanFieldConstants.MediaContents}.content")
                                            .Field($"{VulcanFieldConstants.MediaContents}.content_type"))
                                        .Query(searchText))
                            ),
                        typeFilter: typeRestriction,
                        includeNeutralLanguage: client.Language.Equals(CultureInfo.InvariantCulture),
                        rootReferences: searchRoots,
                        principleReadFilter: UserExtensions.GetUser()
                    );

                    if (clientHits?.Total > 0)
                    {
                        hits.Add(clientHits);
                    }
                }
            }

            var results = hits.SelectMany(h => h.Hits.Select(CreateSearchResult));

            return results;
        }

        /// <summary>
        /// Creates a preview text from a PageData. Will first look for the property MainIntro, and if that's missing, a property called MainBody.
        /// </summary>
        /// <param name="content">The page to extract the preview from.</param>
        protected virtual string CreatePreviewText(IContentData content)
        {
            var str = string.Empty;

            if (content == null)
                return str;

            return TextIndexer.StripHtml(content.Property["MainIntro"] == null ?
                (content.Property["MainBody"] == null ?
                    GetPreviewTextFromFirstLongString(content) : content.Property["MainBody"].ToWebString()) : content.Property["MainIntro"].ToWebString(), 200);
        }

        /// <summary>
        /// Builds result search information for IContent
        /// </summary>
        /// <param name="searchHit"></param>
        /// <returns></returns>
        protected virtual SearchResult CreateSearchResult(IHit<IContent> searchHit)
        {
            Validator.ThrowIfNull(nameof(searchHit), searchHit);

            // load the content from the given link
            var referenceString = (searchHit.Fields["contentLink"] as JArray)?.FirstOrDefault();
            ContentReference reference = null;

            if (referenceString != null)
                ContentReference.TryParse(referenceString.ToString(), out reference);

            if (ContentReference.IsNullOrEmpty(reference))
                throw new Exception("Unable to convert search hit to IContent!");

            var content = ContentRepository.Get<IContent>(reference);
            var localizable = content as ILocalizable;
            var changeTracking = content as IChangeTrackable;
            var editUrl = GetEditUrl(content, out var isOnCurrentHost);
            var result = new SearchResult
            (
                editUrl,
                HttpUtility.HtmlEncode(content.Name),
                CreatePreviewText(content)
            )
            {
                Language = localizable?.Language?.NativeName ?? string.Empty,
                IconCssClass = IconCssClass(content),
                Metadata =
                {
                    ["Id"] = content.ContentLink.ToString(),
                    ["LanguageBranch"] = localizable?.Language?.Name,
                    ["ParentId"] = content.ParentLink.ToString(),
                    ["TypeIdentifier"] = content.GetTypeIdentifier(UiDescriptorRegistry),
                    ["IsOnCurrentHost"] = isOnCurrentHost? "true" : "false"
                }
            };

            var contentType = ContentTypeRepository.Load(content.ContentTypeID);
            CreateToolTip(content, changeTracking, result, contentType);

            return result;
        }

        /// <summary>
        /// Gets the edit URL for a <see cref="T:EPiServer.Core.IContent"/>.
        /// </summary>
        /// <param name="contentData">The content data.</param><param name="onCurrentHost">if set to <c>true</c> current host are used.</param>
        /// <returns>
        /// The edit url.
        /// </returns>
        protected virtual string GetEditUrl(IContent contentData, out bool onCurrentHost)
        {
            var contentLink = contentData.ContentLink;
            var language = contentData is ILocalizable localizable ? localizable.Language.Name : ContentLanguage.PreferredCulture.Name;
            var editUrl = EditPath(contentData, contentLink, language);
            onCurrentHost = true;
            var definitionForContent = SiteDefinitionResolver.GetByContent(contentData.ContentLink, true, true);

            if (definitionForContent?.SiteUrl != SiteDefinition.Current.SiteUrl)
                onCurrentHost = false;

            //if (Settings.Instance.UseLegacyEditMode && typeof(PageData).IsAssignableFrom(typeof(TContent)))
            //    return UriSupport.Combine(UriSupport.Combine(definitionForContent.SiteUrl, settingsFromContent.UIUrl).AbsoluteUri, editUrl);

            return editUrl;
        }

        /// <summary>
        /// Will look for the first long string property, ignoring link collections, that has a value.
        /// </summary>
        /// <param name="content">The page that we want to get a preview for.</param>
        /// <returns>
        /// The value from the first non empty long string.
        /// </returns>
        protected virtual string GetPreviewTextFromFirstLongString(IContentData content)
        {
            foreach (var propertyData in content.Property)
            {
                if (propertyData is PropertyLongString && !(propertyData is PropertyLinkCollection) && !string.IsNullOrEmpty(propertyData.Value as string))
                    return propertyData.ToWebString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the icon CSS class.
        /// </summary>
        protected abstract string IconCssClass(IContent contentData);

        private void CreateToolTip(IContent content, IChangeTrackable changeTracking, SearchResult result, ContentType contentType)
        {
            if (string.IsNullOrEmpty(ToolTipResourceKeyBase))
                return;

            result.ToolTipElements.Add(new ToolTipElement(LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/id", new object[]
            {
                ToolTipResourceKeyBase
            })), content.ContentLink.ToString()));

            if (changeTracking != null)
            {
                result.ToolTipElements.Add(new ToolTipElement(LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/changed", new object[]
                {
                    ToolTipResourceKeyBase
                })), changeTracking.Changed.ToString(CultureInfo.CurrentUICulture)));

                result.ToolTipElements.Add(new ToolTipElement(LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/created", new object[]
                {
                    ToolTipResourceKeyBase
                })), changeTracking.Created.ToString(CultureInfo.CurrentUICulture)));
            }

            if (string.IsNullOrEmpty(ToolTipContentTypeNameResourceKey))
                return;

            result.ToolTipElements.Add
                (new ToolTipElement(LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", new object[]
            {
                ToolTipResourceKeyBase,
                ToolTipContentTypeNameResourceKey
            })), contentType != null ? HttpUtility.HtmlEncode(contentType.LocalizedName) : string.Empty));
        }
    }
}