namespace TcbInternetSolutions.Vulcan.Commerce
{
    using Core;
    using Core.SearchProviders;
    using Core.SearchProviders.Extensions;
    using EPiServer;
    using EPiServer.Commerce.Catalog.ContentTypes;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework.Localization;
    using EPiServer.ServiceLocation;
    using EPiServer.Shell;
    using EPiServer.Shell.Search;
    using EPiServer.Web;

    [SearchProvider]
    public class VulcanCatalogSearchProvider : VulcanSearchProviderBase<EntryContentBase>
    {
        public VulcanCatalogSearchProvider()
              : this(
                    ServiceLocator.Current.GetInstance<IVulcanHandler>(),
                    ServiceLocator.Current.GetInstance<LocalizationService>(),
                    ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>(),
                    ServiceLocator.Current.GetInstance<IContentRepository>(),
                    ServiceLocator.Current.GetInstance<IContentTypeRepository>(),
                    ServiceLocator.Current.GetInstance<UIDescriptorRegistry>()
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
