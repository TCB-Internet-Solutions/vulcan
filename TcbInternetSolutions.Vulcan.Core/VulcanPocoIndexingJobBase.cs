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

        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanPocoIndexingJobBase()
        {
            IsStoppable = true;
        }

        /// <summary>
        /// Poco indexer
        /// </summary>
        protected abstract IVulcanPocoIndexer PocoIndexer { get; set; }

        /// <summary>
        /// Poco indexing job
        /// </summary>
        protected Injected<IVulcanPocoIndexingJob> VulcanPocoIndexHandler { get; set; }

        /// <summary>
        /// Execute poco indexing
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            if (PocoIndexer == null)
                throw new NullReferenceException($"{nameof(PocoIndexer)} cannot be null!");

            var count = 0;
            var result = VulcanPocoIndexHandler.Service.Index(PocoIndexer, OnStatusChanged, ref count, ref _stopSignaled);

            return result;
        }

        /// <summary>
        /// Signal stop
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}
