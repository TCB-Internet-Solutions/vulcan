namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.ServiceLocation;
    using System;
    using TcbInternetSolutions.Vulcan.Core;
    using TcbInternetSolutions.Vulcan.Core.Extensions;
    using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class VulcanAttachmentIndexerInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            context.InitComplete += Context_InitComplete;
        }

        private void Context_InitComplete(object sender, EventArgs e)
        {
            // get all types that derive from MediaData that aren't abstract
            var mediaTypes = typeof(MediaData).GetSearchTypesFor(DefaultFilter);
            var mediaTypeFieldName = MediaContents;
            IVulcanClient client = ServiceLocator.Current.GetInstance<IVulcanClient>();

            // TODO: Dan, how can we set this for all types the derive from MediaData, I figured an init module was the best place
            // also does it need to be supported like other '.analyzed' fields?

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