using EPiServer.Core;
using EPiServer.ServiceLocation;
using System;
using System.IO;
using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    public class VulcanAttachmentIndexModifier : Core.IVulcanIndexingModifier
    {        
        private static Injected<IVulcanAttachmentInspector> _Inspector { get; }

        public void ProcessContent(IContent content, Stream writableStream)
        {
            var media = content as MediaData;

            if (media != null && _Inspector.Service.AllowIndexing(media))
            {
                var streamWriter = new StreamWriter(writableStream);

                if (media != null)
                {
                    streamWriter.Write(",\"" + MediaContents + "\":[");
                    string base64contents = string.Empty;

                    using (var reader = media.BinaryData.OpenRead())
                    {
                        byte[] buffer = new byte[reader.Length];
                        reader.Read(buffer, 0, (int)reader.Length);
                        base64contents = Convert.ToBase64String(buffer);
                    }

                    streamWriter.Write("\"" + base64contents + "\"");
                    streamWriter.Write("]");
                }

                streamWriter.Flush();
            }
        }
    }
}
