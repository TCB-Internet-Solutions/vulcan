namespace TcbInternetSolutions.Vulcan.Core
{
    using Nest;
    using System;

    /// <summary>
    /// Used to customize 
    /// </summary>
    public interface IVulcanCreateIndexCustomizer
    {
        /// <summary>
        /// Null by default, but can be used to setup shards, replicas, anaylzers, etc for the index.
        /// </summary>
        Func<CreateIndexDescriptor, ICreateIndexRequest> CustomizeIndex { get; }

        /// <summary>
        /// Trims not analyzed fields to avoid errors, should never be above 10922, default is 256
        /// <para>See https://www.elastic.co/guide/en/elasticsearch/reference/current/ignore-above.html </para>
        /// </summary>
        int IgnoreAbove { get; }

        /// <summary>
        /// Helps avoid all shards failed on first request.
        /// </summary>
        int WaitForActiveShards { get; }
    }
}
