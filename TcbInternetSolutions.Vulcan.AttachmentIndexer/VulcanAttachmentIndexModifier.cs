using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using System;
using System.IO;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.Core.Extensions;
using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    public class VulcanAttachmentIndexModifier : Core.IVulcanIndexingModifier
    {
        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexModifier));

        public void ProcessContent(IContent content, Stream writableStream)
        {
            var media = content as MediaData;
            var inspector = ServiceLocator.Current.GetInstance<IVulcanAttachmentInspector>();                        

            if (media != null && inspector.AllowIndexing(media))
            {
                Implementation.VulcanAttachmentPropertyMapper.AddMapping(media);

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
