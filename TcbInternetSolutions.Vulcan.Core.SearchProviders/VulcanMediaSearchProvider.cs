namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{
    using Core.Extensions;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework.Localization;
    using EPiServer.ServiceLocation;
    using EPiServer.Shell;
    using EPiServer.Shell.Search;
    using EPiServer.Web;
    using TcbInternetSolutions.Vulcan.Core;

    /// <summary>
    /// UI Search provider for mediadata
    /// </summary>
    [SearchProvider]
    public class VulcanMediaSearchProvider : VulcanSearchProviderBase<MediaData>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanMediaSearchProvider()
              : this(
                    ServiceLocator.Current.GetInstance<IVulcanHandler>(),
                    ServiceLocator.Current.GetInstance<LocalizationService>(),
                    ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>(),
                    ServiceLocator.Current.GetInstance<IContentRepository>(),
                    ServiceLocator.Current.GetInstance<IContentTypeRepository>(),
                    ServiceLocator.Current.GetInstance<UIDescriptorRegistry>()
                )
        { }

        /// <summary>
        /// Injected contructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        /// <param name="localizationService"></param>
        /// <param name="siteDefinitionResolver"></param>
        /// <param name="contentRepository"></param>
        /// <param name="contentTypeRepository"></param>
        /// <param name="uiDescriptorRegistry"></param>
        public VulcanMediaSearchProvider(
            IVulcanHandler vulcanHandler,
            LocalizationService localizationService,
            ISiteDefinitionResolver siteDefinitionResolver,
            IContentRepository contentRepository,
            IContentTypeRepository contentTypeRepository,
            UIDescriptorRegistry uiDescriptorRegistry
        )
          : base(vulcanHandler, contentRepository, contentTypeRepository, localizationService, uiDescriptorRegistry, siteDefinitionResolver)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IncludeInvariant => true;

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>
        /// CMS
        /// </value>
        public override string Area => "CMS/files";

        /// <summary>
        /// Gets the CMS page category.
        /// </summary>
        public override string Category => _LocalizationService.GetString("/vulcan/searchprovider/files/name");

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey => "";

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        protected override string ToolTipResourceKeyBase => "/shell/cms/search/files/tooltip";

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        protected override string IconCssClass(IContent content) => 
            "epi-resourceIcon epi-resourceIcon-" + ContentExtensions.SearchFileExtension(content as MediaData);
    }
}