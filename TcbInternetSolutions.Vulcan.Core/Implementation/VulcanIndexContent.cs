namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.PlugIn;
    using EPiServer.Scheduler;
    using EPiServer.ServiceLocation;
    using Extensions;
    using System;
    using System.Linq;

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
            OnStatusChanged(string.Format("Starting execution of {0}", GetType()));
            
            VulcanHandler.Service.DeleteIndex(); // delete all language indexes
            var indexers = typeof(IVulcanIndexer).GetSearchTypesFor(VulcanFieldConstants.DefaultFilter);
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
