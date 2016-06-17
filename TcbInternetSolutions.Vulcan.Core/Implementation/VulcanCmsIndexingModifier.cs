using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using System;
using System.IO;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {
        public Injected<IContentLoader> ContentLoader { get; set; }

        public void ProcessContent(EPiServer.Core.IContent content, System.IO.Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);
            streamWriter.Write(",\"" + VulcanFieldConstants.Ancestors + "\":[");

            var first = true;

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

            // add MediaData as base64
            //var media = content as MediaData;

            //if (media != null)
            //{
            //    string base64contents = string.Empty;

            //    using (var reader = media.BinaryData.OpenRead())
            //    {
            //        byte[] buffer = new byte[reader.Length];
            //        reader.Read(buffer, 0, (int)reader.Length);
            //        base64contents = Convert.ToBase64String(buffer);
            //    }

            //    // TODO: write to stream for indexing by elastic
            //}

            // add permissions
            var securable = content as ISecurable;

            if (securable != null)
            {
                var repo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
                var access = repo.Get(content.ContentLink);

                //access.Entries.First().Access

                //new EPiServer.Security.AccessControlList().
                //securable.GetSecurityDescriptor().
            }


            streamWriter.Flush();
        }
    }
}
