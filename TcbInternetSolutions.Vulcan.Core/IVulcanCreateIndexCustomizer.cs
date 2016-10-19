namespace TcbInternetSolutions.Vulcan.Core
{
    using Nest;
    using System;

    public interface IVulcanCreateIndexCustomizer
    {
        /// <summary>
        /// Null by default, but can be used to create custom analyzers
        /// </summary>
        Func<CreateIndexDescriptor, ICreateIndexRequest> CustomizeIndex { get; }

        /// <summary>
        /// Trims not analyzed fields to avoid errors, should never be above 10922, default is 256
        /// <para>See https://www.elastic.co/guide/en/elasticsearch/reference/current/ignore-above.html </para>
        /// </summary>
        int IgnoreAbove { get; }
    }
}
