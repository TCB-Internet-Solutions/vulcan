using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace TcbInternetSolutions.Vulcan.Core.Initialization
{
    /// <summary>
    /// Registers implementations to DI container
    /// </summary>
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class RegisterImplementations : IConfigurableModule, IInitializableModule
    {
        void IConfigurableModule.ConfigureContainer(ServiceConfigurationContext context)
        {
            // hack: using manual registration as scheduled job doesn't inject otherwise
            context.Services.AddSingleton<IVulcanIndexer, Implementation.VulcanCmsIndexer>();
        }

        void IInitializableModule.Initialize(InitializationEngine context) { }

        void IInitializableModule.Uninitialize(InitializationEngine context) { }
    }
}
