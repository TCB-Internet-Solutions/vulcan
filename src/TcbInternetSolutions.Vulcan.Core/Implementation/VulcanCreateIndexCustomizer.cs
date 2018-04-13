namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.ServiceLocation;
    using Nest;
    using System;

    /// <summary>
    /// Allows for customization of index creation
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanCreateIndexCustomizer), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCreateIndexCustomizer : IVulcanCreateIndexCustomizer
    {
        /// <summary>
        /// Customization of create index
        /// </summary>
        public virtual Func<CreateIndexDescriptor, ICreateIndexRequest> CustomizeIndex => null;

        /// <summary>
        /// Default ignore above for stored, not analyzed strings
        /// </summary>
        public virtual int IgnoreAbove => 256;

        /// <summary>
        /// Wait for active shards
        /// </summary>
        public virtual int WaitForActiveShards => 1;
    }
}