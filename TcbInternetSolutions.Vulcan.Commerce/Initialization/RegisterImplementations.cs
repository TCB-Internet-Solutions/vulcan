using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce.Initialization
{
    /// <summary>
    /// Registers implementations to DI container
    /// </summary>
    [ModuleDependency(typeof(Core.Initialization.RegisterImplementations))]
    public class RegisterImplementations : IConfigurableModule
    {
        void IConfigurableModule.ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IVulcanIndexer, VulcanCommerceIndexer>();
        }

        void IInitializableModule.Initialize(InitializationEngine context) { }

        void IInitializableModule.Uninitialize(InitializationEngine context) { }
    }
}
