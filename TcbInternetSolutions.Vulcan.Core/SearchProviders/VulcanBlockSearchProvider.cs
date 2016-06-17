namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework.Localization;
    using EPiServer.ServiceLocation;
    using EPiServer.Shell;
    using EPiServer.Shell.Search;
    using TcbInternetSolutions.Vulcan.Core;    

    //TODO: Figure out how to handle block only searches when there is an IContent type restriction

    [SearchProvider]
    public class VulcanBlockSearchProvider : VulcanSearchProviderBase<BlockData, BlockType>
    {
        public VulcanBlockSearchProvider()
              : this(
                    ServiceLocator.Current.GetInstance<IVulcanHandler>(),
                    ServiceLocator.Current.GetInstance<LocalizationService>(),
                    ServiceLocator.Current.GetInstance<IEnterpriseSettings>(),
                    ServiceLocator.Current.GetInstance<IContentRepository>(),
                    ServiceLocator.Current.GetInstance<IContentTypeRepository>(),
                    ServiceLocator.Current.GetInstance<UIDescriptorRegistry>()
                )
        { }

        public VulcanBlockSearchProvider(
            IVulcanHandler vulcanHandler,
            LocalizationService localizationService,
            IEnterpriseSettings enterpriseSettings,
            IContentRepository contentRepository,
            IContentTypeRepository contentTypeRepository,
            UIDescriptorRegistry uiDescriptorRegistry
        )
          : base(vulcanHandler, contentRepository, contentTypeRepository, localizationService, uiDescriptorRegistry, enterpriseSettings)
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
        public override string Category => _LocalizationService.GetString("/vulcan/searchprovider/blocks/name");

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey => "blocktype";

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        protected override string ToolTipResourceKeyBase => "/shell/cms/search/blocks/category";

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        protected override string IconCssClass(BlockData pageData) => "epi-resourceIcon epi-resourceIcon-block";
    }
}