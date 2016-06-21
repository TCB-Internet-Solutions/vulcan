namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{
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
    using Implementation;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Web;
    using TcbInternetSolutions.Vulcan.Core.Extensions;

    public abstract class VulcanSearchProviderBase<TContent, TContentType> :
        ISearchProvider, ISortable where TContent : class, IContent where TContentType : ContentType
    {
        /// <summary>
        /// Link for the search hit, which should be a link to the edit page for the content.
        /// </summary>
        public Func<IContent, ContentReference, string, string> EditPath;

        protected IContentRepository _ContentRepository;

        protected IContentTypeRepository _ContentTypeRepository;

        protected SiteDefinitionResolver _SiteDefinitionResolver;

        protected LocalizationService _LocalizationService;

        protected UIDescriptorRegistry _UIDescriptorRegistry;

        protected IVulcanHandler _VulcanHandler;

        public VulcanSearchProviderBase(IVulcanHandler vulcanHandler, IContentRepository contentRepository, IContentTypeRepository contentTypeRepository, LocalizationService localizationService, UIDescriptorRegistry uiDescriptorRegistry, SiteDefinitionResolver enterpriseSettings)
        {
            _VulcanHandler = vulcanHandler;
            _ContentRepository = contentRepository;
            _ContentTypeRepository = contentTypeRepository;
            _LocalizationService = localizationService;
            _UIDescriptorRegistry = uiDescriptorRegistry;
            _SiteDefinitionResolver = enterpriseSettings;

            EditPath = (contentData, contentLink, languageName) =>
            {
                string fullUrlToEditView = SearchProviderExtensions.GetFullUrlToEditView(_SiteDefinitionResolver.GetDefinitionForContent(contentLink, false, false), null);
                Uri uri = SearchProviderExtensions.GetUri(contentData);

                if (!string.IsNullOrWhiteSpace(languageName))
                    return string.Format("{0}?language={1}#context={2}", fullUrlToEditView, languageName, uri);

                return string.Format("{0}?#context={1}", fullUrlToEditView, uri);
            };
        }

        public abstract string Area { get; }

        public abstract string Category { get; }

        public virtual int SortOrder => 99;

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
                ContentReference c = null;

                foreach (var item in query.SearchRoots)
                {
                    if (ContentReference.TryParse(item, out c))
                        searchRoots.Add(c);
                }
            }

            // TODO: Add in permission filtering
            
            ISearchResponse<IContent> hits;

            // Special condition for BlockData since it doesn't derive from BlockData
            if (typeof(TContent) == typeof(VulcanContentHit))
            {
                var typeRestriction = typeof(BlockData).GetSearchTypesFor(removeAbstractClasses: true);

                hits = _VulcanHandler.GetClient().SearchContent<IContent>(d => d
                        .Take(query.MaxResults)
                        .Query(q => q.SimpleQueryString(sq => sq.Fields(fields => fields.Field("*.analyzed")).Query(searchText))),
                        includeNeutralLanguage: IncludeInvariant,
                        rootReferences: searchRoots,
                        typeFilter: typeRestriction
                );
            }
            else
            {
                hits = _VulcanHandler.GetClient().SearchContent<TContent>(d => d
                        .Take(query.MaxResults)
                        //.Query(q => q.QueryString(sq => sq.Query(searchText))),
                        .Query(q => q.SimpleQueryString(sq => sq.Fields(fields => fields.Field("*.analyzed")).Query(searchText))),
                        includeNeutralLanguage: IncludeInvariant,
                        rootReferences: searchRoots
                );
            }
            
            var results = hits.Hits.Select(x => CreateSearchResult(x.Source));

            return results;
        }

        /// <summary>
        /// Creates a preview text from a PageData. Will first look for the property MainIntro, and if that's missing, a property called MainBody.
        /// </summary>
        /// <param name="content">The page to extract the preview from.</param>
        protected virtual string CreatePreviewText(IContentData content)
        {
            string str = string.Empty;

            if (content == null)
                return str;

            return TextIndexer.StripHtml(content.Property["MainIntro"] == null ?
                (content.Property["MainBody"] == null ?
                    GetPreviewTextFromFirstLongString(content) : content.Property["MainBody"].ToWebString()) : content.Property["MainIntro"].ToWebString(), 200);
        }

        /// <summary>
        /// Builds result search information for IContent
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected virtual SearchResult CreateSearchResult(IContent content)
        {
            Validator.ThrowIfNull(nameof(content), content);
            // TODO: look into vulcan issue not setting IContent correctly
            content = _ContentRepository.Get<IContent>(content.ContentLink); // reload as Vulcan isn't returning properties correctly

            ILocalizable localizable = content as ILocalizable;
            IChangeTrackable changeTracking = content as IChangeTrackable;

            if (content == null)
                throw new ArgumentException(string.Format("Argument {0} must implement interface EPiServer.Core.IContent", "content"));

            bool onCurrentHost;
            SearchResult result = new SearchResult
            (
                GetEditUrl(content, out onCurrentHost),
                HttpUtility.HtmlEncode(content.Name),
                CreatePreviewText(content)
            );

            result.IconCssClass = IconCssClass((TContent)content);
            result.Metadata["Id"] = content.ContentLink.ToString();
            result.Metadata["LanguageBranch"] = localizable == null || localizable.Language == null ? string.Empty : localizable.Language.Name;
            result.Metadata["ParentId"] = content.ParentLink.ToString();
            result.Metadata["IsOnCurrentHost"] = onCurrentHost ? "true" : "false";
            result.Metadata["TypeIdentifier"] = SearchProviderExtensions.GetTypeIdentifier(content, _UIDescriptorRegistry);
            ContentType contentType = _ContentTypeRepository.Load(content.ContentTypeID);

            CreateToolTip(content, changeTracking, result, contentType);
            result.Language = localizable == null || localizable.Language == null ? string.Empty : localizable.Language.NativeName;

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
            ContentReference contentLink = contentData.ContentLink;
            ILocalizable localizable = contentData as ILocalizable;
            string language = localizable != null ? localizable.Language.Name : ContentLanguage.PreferredCulture.Name;
            string editUrl = EditPath(contentData, contentLink, language);
            onCurrentHost = true;
            SiteDefinition definitionForContent = _SiteDefinitionResolver.GetDefinitionForContent(contentData.ContentLink, false, false);

            if (definitionForContent.SiteUrl != SiteDefinition.Current.SiteUrl)
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
            foreach (PropertyData propertyData in content.Property)
            {
                if (propertyData is PropertyLongString && !(propertyData is PropertyLinkCollection) && !string.IsNullOrEmpty(propertyData.Value as string))
                    return propertyData.ToWebString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the icon CSS class.
        /// </summary>
        protected abstract string IconCssClass(TContent contentData);

        private void CreateToolTip(IContent content, IChangeTrackable changeTracking, SearchResult result, ContentType contentType)
        {
            if (string.IsNullOrEmpty(ToolTipResourceKeyBase))
                return;

            result.ToolTipElements.Add(new ToolTipElement(_LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/id", new object[1]
            {
                ToolTipResourceKeyBase
            })), content.ContentLink.ToString()));

            if (changeTracking != null)
            {
                result.ToolTipElements.Add(new ToolTipElement(_LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/changed", new object[1]
                {
                    ToolTipResourceKeyBase
                })), changeTracking.Changed.ToString()));

                result.ToolTipElements.Add(new ToolTipElement(_LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/created", new object[1]
                {
                    ToolTipResourceKeyBase
                })), changeTracking.Created.ToString()));
            }

            if (string.IsNullOrEmpty(ToolTipContentTypeNameResourceKey))
                return;

            result.ToolTipElements.Add
                (new ToolTipElement(_LocalizationService.GetString(string.Format(CultureInfo.InvariantCulture, "{0}/{1}", new object[2]
            {
                ToolTipResourceKeyBase,
                ToolTipContentTypeNameResourceKey
            })), contentType != null ? HttpUtility.HtmlEncode(contentType.LocalizedName) : string.Empty));
        }
    }
}