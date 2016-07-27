namespace TcbInternetSolutions.Vulcan.Core
{
    using System;
    using EPiServer.Scheduler;
    using EPiServer.ServiceLocation;

    /// <summary>
    /// Base class to allow creating individual poco indexing jobs per indexer.
    /// </summary>
    public abstract class VulcanPocoIndexingJobBase : ScheduledJobBase
    {
        private bool _stopSignaled;

        public VulcanPocoIndexingJobBase()
        {
            IsStoppable = true;
        }

        protected abstract IVulcanPocoIndexer PocoIndexer { get; set; }

        protected Injected<IVulcanPocoIndexingJob> VulcanPocoIndexHandler { get; set; }

        public override string Execute()
        {
            if (PocoIndexer == null)
                throw new NullReferenceException($"{nameof(PocoIndexer)} cannot be null!");

            var count = 0;
            var result = VulcanPocoIndexHandler.Service.Index(PocoIndexer, OnStatusChanged, ref count, ref _stopSignaled);

            return result;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}
