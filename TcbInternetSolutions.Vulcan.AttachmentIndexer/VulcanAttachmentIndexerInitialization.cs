namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using Implementation;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Web;
    using TcbInternetSolutions.Vulcan.Core;

    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class VulcanAttachmentIndexerInitialization : IInitializableModule
    {
        private HostType CurrentHostType = HostType.Undefined;

        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexerInitialization));

        public void Initialize(InitializationEngine context)
        {
            CurrentHostType = HostType.WebApplication;
            context.InitComplete += Context_InitComplete;
        }

        private void Context_InitComplete(object sender, EventArgs e)
        {
            IVulcanHandler handler = ServiceLocator.Current.GetInstance<IVulcanHandler>();

            // Clear static list so property mapping can be re-created.
            handler.DeletedIndices += ((IEnumerable<string> deletedIndices) =>
            {
                VulcanAttachmentPropertyMapper.AddedMappings.Clear();
            });

            IVulcanClient client = handler.GetClient(CultureInfo.InvariantCulture);
            var info = client.NodesInfo();

            if (info?.Nodes?.Any(x => x.Value?.Plugins?.Any(y => string.Compare(y.Name, "mapper-attachments", true) == 0) == true) != true)
            {
                if (CurrentHostType != HostType.WebApplication ||
                    (CurrentHostType == HostType.WebApplication && HttpContext.Current?.IsDebuggingEnabled == true))
                {
                    // Only throw exception if not a web application or is a web application with debug turned on
                    throw new Exception("No attachment plugin found, be sure to install the 'mapper-attachments' plugin on your Elastic Search Server!");
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}