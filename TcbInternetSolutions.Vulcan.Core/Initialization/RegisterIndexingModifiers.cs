using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TcbInternetSolutions.Vulcan.Core.Extensions;

namespace TcbInternetSolutions.Vulcan.Core.Initialization
{
    /// <summary>
    /// Registers IVulcanIndexingModifier with DI Container
    /// </summary>
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class RegisterIndexingModifiers : IConfigurableModule, IInitializableModule
    {
        void IConfigurableModule.ConfigureContainer(ServiceConfigurationContext context)
        {
            var modifierType = typeof(IVulcanIndexingModifier);
            var indexModifierTypes = modifierType.GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);

            foreach (var type in indexModifierTypes)
            {
                context.Services.AddSingleton(modifierType, type);
            }
        }

        void IInitializableModule.Initialize(InitializationEngine context)
        {

        }

        void IInitializableModule.Uninitialize(InitializationEngine context)
        {

        }
    }
}
