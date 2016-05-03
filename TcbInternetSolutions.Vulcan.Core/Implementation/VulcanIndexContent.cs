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
using EPiServer.DataAbstraction;

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

            VulcanHandler.Service.DeleteIndex(); // delete all language indexes

            var indexers = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                indexers.AddRange(assembly.GetTypes().Where(t => typeof(IVulcanIndexer).IsAssignableFrom(t) && t.IsClass));
            }

            var count = 0;
            
            for (int i = 0; i < indexers.Count; i++)
            {
                var indexer = (IVulcanIndexer)Activator.CreateInstance(indexers[i]);

                var contentReferences = ContentLoader.Service.GetDescendents(indexer.GetRoot().Key);

                for(int cr = 0; cr < contentReferences.Count(); cr++)
                {
                    OnStatusChanged("Indexing item " + (cr + 1).ToString() + " of " + contentReferences.Count() + " items of " + indexer.GetRoot().Value + " content (indexer " + (i + 1).ToString() + " of " + indexers.Count.ToString() + ")...");

                    VulcanHandler.Service.IndexContentEveryLanguage(ContentLoader.Service.Get<IContent>(contentReferences.ElementAt(cr)));

                    if (_stopSignaled)
                    {
                        return "Stop of job was called";
                    }

                    count++;
                }
            }

            return "Vulcan successfully indexed " + count.ToString() + " item(s)";
        }
    }
}
