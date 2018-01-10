﻿namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
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

    /// <summary>
    /// Init module to determine if mapper-attachments is available
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class VulcanAttachmentIndexerInitialization : IInitializableModule
    {
        // 5.x support uses https://www.elastic.co/guide/en/elasticsearch/plugins/5.2/ingest-attachment.html
        // not which 2.x uses https://www.elastic.co/guide/en/elasticsearch/plugins/5.2/mapper-attachments.html
        private const bool _isVersion5 = true;
        private static readonly string _pluginName = _isVersion5 ? "ingest-attachment" : "mapper-attachments"; // older 2.x

        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexerInitialization));
        private HostType CurrentHostType = HostType.Undefined;

        /// <summary>
        /// Injectable attachment settings
        /// </summary>
        public Injected<IVulcanAttachmentIndexerSettings> AttachmentSettings { get; set; }

        /// <summary>
        /// Injectable IVulcanHandler
        /// </summary>
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        void IInitializableModule.Initialize(InitializationEngine context)
        {
            CurrentHostType = HostType.WebApplication;
            context.InitComplete += Context_InitComplete;
        }

        void IInitializableModule.Uninitialize(InitializationEngine context) { }

        private void Context_InitComplete(object sender, EventArgs e)
        {
            // Clear static list so property mapping can be re-created.
            VulcanHandler.Service.DeletedIndices += ((IEnumerable<string> deletedIndices) =>
            {
                VulcanAttachmentPropertyMapper.AddedMappings.Clear();
            });

            if (AttachmentSettings.Service.EnableAttachmentPlugins)
            {
                IVulcanClient client = VulcanHandler.Service.GetClient(CultureInfo.InvariantCulture);
                var info = client.NodesInfo();

                if (info?.Nodes?.Any(x => x.Value?.Plugins?.Any(y => string.Compare(y.Name, _pluginName, true) == 0) == true) != true)
                {
                    if (CurrentHostType != HostType.WebApplication ||
                        (CurrentHostType == HostType.WebApplication && HttpContext.Current?.IsDebuggingEnabled == true))
                    {
                        // Only throw exception if not a web application or is a web application with debug turned on
                        throw new Exception($"No attachment plugin found, be sure to install the '{_pluginName}' plugin on your Elastic Search Server!");
                    }
                }
            }
        }
    }
}