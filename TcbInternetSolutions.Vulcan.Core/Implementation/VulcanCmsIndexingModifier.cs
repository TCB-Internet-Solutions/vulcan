using EPiServer;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Core;

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

            streamWriter.Flush();
        }
    }
}
