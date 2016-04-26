using System;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer;
using EPiServer.Web;
using Elasticsearch.Net;
using Nest;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.Logging;
using System.Linq;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    [ScheduledPlugIn(DisplayName = "Vulcan Index Content")]
    public class VulcanIndexContent : ScheduledJobBase
    {
        private static ILogger Logger = LogManager.GetLogger();

        private bool _stopSignaled;

        public Injected<IContentLoader> ContentLoader { get; set; }
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public VulcanIndexContent()
        {
            IsStoppable = true;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            OnStatusChanged(String.Format("Starting execution of {0}", this.GetType()));

            VulcanHelper.DeleteIndex();

            var client = VulcanHelper.GetClient();

            var indexers = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                indexers.AddRange(assembly.GetTypes().Where(t => typeof(IVulcanIndexer).IsAssignableFrom(t) && t.IsClass));
            }
            
            foreach(var indexer in indexers)
            {
                var indexerObject = (IVulcanIndexer)Activator.CreateInstance(indexer);

                var root = ContentLoader.Service.Get<IContent>(indexerObject.GetRoot().Key);

                OnStatusChanged("Indexing " + indexerObject.GetRoot().Value + " content...");

                var complete = IndexIncremental(client, root);

                if(!complete)
                {
                    return "Stop of job was called";
                }
            }

            return "Vulcan successfully completed.";
        }

        private bool IndexIncremental(ElasticClient client, IContent content)
        {
            if (_stopSignaled)
            {
                return false;
            }

            VulcanHandler.Service.Client.IndexContent(content);

            foreach(var child in ContentLoader.Service.GetChildren<IContent>(content.ContentLink))
            {
                var complete = IndexIncremental(client, child);

                if (!complete) return false;
            }

            return true;
        }
    }
}
