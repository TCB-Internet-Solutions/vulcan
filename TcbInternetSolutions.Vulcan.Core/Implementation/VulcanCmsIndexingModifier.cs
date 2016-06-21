namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.ServiceLocation;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {
        public Injected<IContentLoader> ContentLoader { get; set; }

        public virtual void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            var first = true;
            var streamWriter = new StreamWriter(writableStream);
            streamWriter.Write(",\"" + VulcanFieldConstants.Ancestors + "\":[");

            foreach (var ancestor in ContentLoader.Service.GetAncestors(content.ContentLink))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    streamWriter.Write(",");
                }
                streamWriter.Write("\"" + ancestor.ContentLink.ToReferenceWithoutVersion().ToString() + "\"");
            }

            streamWriter.Write("]");

            // index read permission
            streamWriter.Write(",\"" + VulcanFieldConstants.ReadPermission + "\":[");

            first = true;
            var repo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            var permissions = repo.Get(content.ContentLink);

            foreach (var access in permissions.Entries)
            {
                if (access.Access.HasFlag(EPiServer.Security.AccessLevel.Read) ||
                    access.Access.HasFlag(EPiServer.Security.AccessLevel.Administer) ||
                    access.Access.HasFlag(EPiServer.Security.AccessLevel.FullAccess))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        streamWriter.Write(",");
                    }

                    streamWriter.Write("\"" + access.Name + "\"");
                }

            }

            streamWriter.Write("]");

            // index VulcanSearchableAttribute
            streamWriter.Write(",\"" + VulcanFieldConstants.CustomContents + "\":[");
            first = true;
            var properties = content.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(VulcanSearchableAttribute)));

            foreach (var p in properties)
            {
                object value = p.GetValue(content);

                if (p.PropertyType == typeof(ContentArea))
                {
                    value = Extensions.ContentAreaExtensions.GetContentAreaContents(value as ContentArea);
                }

                if (value != null)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        streamWriter.Write(",");
                    }

                    streamWriter.Write("\"" + value.ToString() + "\"");
                }
            }

            streamWriter.Flush();
        }
    }
}