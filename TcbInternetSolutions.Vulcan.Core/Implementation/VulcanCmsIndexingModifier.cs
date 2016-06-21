using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using System.IO;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {
        public Injected<IContentLoader> ContentLoader { get; set; }

        public void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
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
            streamWriter.Flush();
        }
    }
}
