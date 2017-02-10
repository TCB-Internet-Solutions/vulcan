using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Vulcan content hit
    /// </summary>
    public class VulcanContentHit : IVulcanContentHit
    {
        /// <summary>
        /// Content Guid
        /// </summary>
        public virtual Guid ContentGuid { get; set; }

        /// <summary>
        /// Episerver content reference
        /// </summary>
        public virtual ContentReference ContentLink { get; set; }

        /// <summary>
        /// Content type id
        /// </summary>
        public virtual int ContentTypeID { get; set; }

        /// <summary>
        /// Content is deleted
        /// </summary>
        public virtual bool IsDeleted { get; set; }

        /// <summary>
        /// Content name
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Content parent reference
        /// </summary>
        public virtual ContentReference ParentLink { get; set; }

        /// <summary>
        /// Content properties
        /// </summary>
        public PropertyDataCollection Property { get; set; }

        /// <summary>
        /// Prices for commerce
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __prices { get; set; }

        /// <summary>
        /// Low prices for commerce
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __pricesLow { get; set; }

        /// <summary>
        /// High prices for commerce
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __pricesHigh { get; set; }
    }
}
