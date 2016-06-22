using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanContentHit : IVulcanContentHit
    {
        public Guid ContentGuid { get; set; }

        public ContentReference ContentLink { get; set; }

        public int ContentTypeID { get; set; }

        public bool IsDeleted { get; set; }

        public string Name { get; set; }

        public ContentReference ParentLink { get; set; }

        public PropertyDataCollection Property { get; set; }

        /* these are used by commerce, but in the core so that they are available to all search hits */
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __prices { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __pricesLow { get; set; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<string, decimal> __pricesHigh { get; set; }
    }
}
