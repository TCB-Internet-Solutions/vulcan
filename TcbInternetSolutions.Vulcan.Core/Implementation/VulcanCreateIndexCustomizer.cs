namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.ServiceLocation;
    using Nest;
    using System;

    [ServiceConfiguration(typeof(IVulcanCreateIndexCustomizer), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCreateIndexCustomizer : IVulcanCreateIndexCustomizer
    {
        public virtual Func<CreateIndexDescriptor, ICreateIndexRequest> CustomizeIndex => null;

        public virtual int IgnoreAbove => 256;

        public virtual int WaitForActiveShards => 1;
    }
}