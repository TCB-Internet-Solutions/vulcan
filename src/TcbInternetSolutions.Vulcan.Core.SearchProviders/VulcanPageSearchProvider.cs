using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Core.SearchProviders
{

    /// <summary>
    /// UI search provider for PageData
    /// </summary>
    [SearchProvider]
    public class VulcanPageSearchProvider : VulcanSearchProviderBase<PageData>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanPageSearchProvider()
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
        /// Injected contructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        /// <param name="localizationService"></param>
        /// <param name="siteDefintionResolver"></param>
        /// <param name="contentRepository"></param>
        /// <param name="contentTypeRepository"></param>
        /// <param name="uiDescriptorRegistry"></param>
        public VulcanPageSearchProvider(
            IVulcanHandler vulcanHandler,
            LocalizationService localizationService,
            ISiteDefinitionResolver siteDefintionResolver,
            IContentRepository contentRepository,
            IContentTypeRepository contentTypeRepository,
            UIDescriptorRegistry uiDescriptorRegistry
        )
          : base(vulcanHandler, contentRepository, contentTypeRepository, localizationService, uiDescriptorRegistry, siteDefintionResolver)
        {

        }

        /// <summary>
        /// Area that the provider maps to, used for spotlight searching
        /// </summary>
        /// <value>
        /// CMS
        /// </value>
        public override string Area => "CMS/pages";

        /// <summary>
        /// Gets the CMS page category.
        /// </summary>
        public override string Category => LocalizationService.GetString("/vulcan/searchprovider/pages/name");

        /// <summary>
        /// Gets the name of the localization page type.
        /// </summary>
        protected override string ToolTipContentTypeNameResourceKey => "pagetype";

        /// <summary>
        /// Gets the page localization path.
        /// </summary>
        protected override string ToolTipResourceKeyBase => "/shell/cms/search/pages/tooltip";

        /// <summary>
        /// Gets the icon CSS class for pages.
        /// </summary>
        protected override string IconCssClass(IContent pageData) => "epi-resourceIcon epi-resourceIcon-page";
    }
}