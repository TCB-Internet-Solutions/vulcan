using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.Core.Implementation;
using TcbInternetSolutions.Vulcan.Core.SearchProviders;
using TcbInternetSolutions.Vulcan.Core.SearchProviders.Extensions;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    [SearchProvider]
    public class VulcanCatalogSearchProvider : VulcanSearchProviderBase<EntryContentBase>
    {
        public VulcanCatalogSearchProvider()
              : this(
                    VulcanHelper.GetService<IVulcanHandler>(),
                    VulcanHelper.GetService<LocalizationService>(),
                    VulcanHelper.GetService<ISiteDefinitionResolver>(),
                    VulcanHelper.GetService<IContentRepository>(),
                    VulcanHelper.GetService<IContentTypeRepository>(),
                    VulcanHelper.GetService<UIDescriptorRegistry>()
                )
        { }

        public VulcanCatalogSearchProvider(
            IVulcanHandler vulcanHandler,
            LocalizationService localizationService,
            ISiteDefinitionResolver siteDefinitionResolver,
            IContentRepository contentRepository,
            IContentTypeRepository contentTypeRepository,
            UIDescriptorRegistry uiDescriptorRegistry
        )
          : base(vulcanHandler, contentRepository, contentTypeRepository, localizationService, uiDescriptorRegistry, siteDefinitionResolver)
        {
            EditPath = GetEditPath;
        }

        public override string Area => "Commerce/Catalog";

        public override string Category => LocalizationService.GetString("/vulcan/searchprovider/products/name");

        protected override string IconCssClass(IContent contentData) => "epi-resourceIcon epi-resourceIcon-page";

        private static string GetEditPath(IContent entryContent, ContentReference contentLink, string languageName)
        {
            return entryContent.GetUri();
        }
    }
}
