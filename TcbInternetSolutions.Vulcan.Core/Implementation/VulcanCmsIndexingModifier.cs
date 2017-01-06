namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web;

    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {
        public Injected<IContentLoader> ContentLoader { get; set; }

        public virtual void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);
            var ancestors = ContentLoader.Service.GetAncestors(content.ContentLink);

            // index ancestors
            streamWriter.Write(",\"" + VulcanFieldConstants.Ancestors + "\":[");
            streamWriter.Write(string.Join(",", ancestors.Select(x => "\"" + x.ContentLink.ToReferenceWithoutVersion().ToString() + "\"")));
            streamWriter.Write("]");
            
            // index read permission
            var repo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            var permissions = repo.Get(content.ContentLink);

            if (permissions != null) // will be null for commerce products, compatibility handled in commerce modifier
            {
                streamWriter.Write(",\"" + VulcanFieldConstants.ReadPermission + "\":[");
                streamWriter.Write(string.Join(",", permissions.Entries.
                            Where(x =>
                                x.Access.HasFlag(AccessLevel.Read) ||
                                x.Access.HasFlag(AccessLevel.Administer) ||
                                x.Access.HasFlag(AccessLevel.FullAccess))
                            .Select(x => "\"" + HttpUtility.JavaScriptStringEncode(x.Name) + "\"")
                        ));
                streamWriter.Write("]");
            }

            // index VulcanSearchableAttribute
            List<string> contents = new List<string>();
            var properties = content.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(VulcanSearchableAttribute)));

            foreach (var p in properties)
            {
                object value = p.GetValue(content);

                // Property to string conversions
                if (p.PropertyType == typeof(ContentArea))
                {
                    value = Extensions.ContentAreaExtensions.GetContentAreaContents(value as ContentArea);
                }

                string v = value?.ToString();

                if (!string.IsNullOrWhiteSpace(v))
                {
                    contents.Add(v);
                }
            }

            streamWriter.Write(",\"" + VulcanFieldConstants.CustomContents + "\":\"" + string.Join(" ", contents) + "\"");
            //streamWriter.Write(",\"" + VulcanFieldConstants.CustomContents + "\":[");
            //streamWriter.Write(string.Join(",", contents.Select(value => "\"" + value.ToString() + "\"")));
            //streamWriter.Write("]");

            streamWriter.Flush();
        }
    }
}