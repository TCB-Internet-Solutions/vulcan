namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using System;
    using System.Linq;
    using System.Web;
    using TcbInternetSolutions.Vulcan.Core;
    using TcbInternetSolutions.Vulcan.Core.Extensions;
    using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

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
                        
            IVulcanClient client = ServiceLocator.Current.GetInstance<IVulcanHandler>().GetClient();
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

            // TODO: Dan, how can we set this for all types the derive from MediaData, I figured an init module was the best place
            
            // get all types that derive from MediaData that aren't abstract
            var mediaTypes = typeof(MediaData).GetSearchTypesFor(DefaultFilter);

            foreach (Type media in mediaTypes.Where(x => x != typeof(MediaData)))
            {
                try
                {
                    var response = client.Map<object>(m => m.
                        Index("_all").
                        Type(media.FullName).
                            Properties(props => props.
                                Attachment(s => s.Name(MediaContents)))
                        );

                }
                catch(Exception ex)
                {
                    _Logger.Error("Failed to map attachment field for type: " + media.FullName, ex);
                }

            }
            
            //POST /To-Index-Url
            //{
            //    "mappings": {
            //        "person": {
            //            "properties": {
            //                "__mediaContents": { "type": "attachment" }
            //            }
            //        }
            //    }
            //}
        }

        public void Uninitialize(InitializationEngine context)
        {

        }
    }
}