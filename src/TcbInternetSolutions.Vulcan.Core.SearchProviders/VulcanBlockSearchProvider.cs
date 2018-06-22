namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{
    using Core;
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework.Localization;
    using EPiServer.Shell;
    using EPiServer.Shell.Search;
    using EPiServer.Web;
    using Implementation;

    /// <summary>
    /// UI Search provider for blocks
    /// </summary>
    [SearchProvider]
    public class VulcanBlockSearchProvider : VulcanSearchProviderBase<VulcanContentHit> // using VulcanContentHit due to IContent restriction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanBlockSearchProvider()
              : this(
                    VulcanHelper.GetService<IVulcanHandler>(),
                    VulcanHelper.GetService<LocalizationService>(),
                    VulcanHelper.GetService<ISiteDefinitionResolver>(),
                    VulcanHelper.GetService<IContentRepository>(),
                    VulcanHelper.GetService<IContentTypeRepository>(),
                    VulcanHelper.GetService<UIDescriptorRegistry>()
                )
        { }

        /// <summary>
        /// Injectable constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        /// <param name="localizationService"></param>
        /// <param name="siteDefinitionResolver"></param>
        /// <param name="contentRepository"></param>
        /// <param name="contentTypeRepository"></param>
        /// <param name="uiDescriptorRegistry"></param>
        public VulcanBlockSearchProvider(
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
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>
        /// CMS
        /// </value>
        public override string Area => "CMS/blocks";

        /// <summary>
        /// Gets the CMS page category.
        /// </summary>
        public override string Category => LocalizationService.GetString("/vulcan/searchprovider/blocks/name");

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey => "blocktype";

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        protected override string ToolTipResourceKeyBase => "/shell/cms/search/blocks/tooltip";

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        protected override string IconCssClass(IContent pageData) => "epi-resourceIcon epi-resourceIcon-block";
    }
}