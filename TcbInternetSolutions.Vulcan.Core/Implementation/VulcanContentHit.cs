using EPiServer.Core;
using System;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanContentHit : IVulcanContentHit
    {
        public virtual Guid ContentGuid { get; set; }

        public virtual ContentReference ContentLink { get; set; }

        public virtual int ContentTypeID { get; set; }

        public virtual bool IsDeleted { get; set; }

        public virtual string Name { get; set; }

        public virtual ContentReference ParentLink { get; set; }

        public virtual PropertyDataCollection Property { get; set; }
    }
}
