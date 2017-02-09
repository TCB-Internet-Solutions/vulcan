namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using System;
    using System.IO;
    using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;
    using System.Collections.Generic;
    using TcbInternetSolutions.Vulcan.Core.Extensions;

    /// <summary>
    /// Adds attachment content to serialized data
    /// </summary>
    public class VulcanAttachmentIndexModifier : Core.IVulcanIndexingModifier
    {
        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexModifier));

        static Injected<IVulcanAttachmentInspector> _AttachmentInspector;

        static Injected<IVulcanAttachmentIndexerSettings> _AttachmentSettings;

        static Injected<IVulcanMediaReader> _MediaReader;

        /// <summary>
        /// Gets ancestors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<ContentReference> GetAncestors(IContent content) => null;

        /// <summary>
        /// Adds attachment content to serialized data
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        public void ProcessContent(IContent content, Stream writableStream)
        {
            var media = content as MediaData;

            if (media != null && _AttachmentInspector.Service.AllowIndexing(media))
            {
                var streamWriter = new StreamWriter(writableStream);
                byte[] mediaBytes = _MediaReader.Service.ReadToEnd(media);
                string mimeType = media.MimeType;

                if (mediaBytes?.LongLength > 0)
                {
                    if (_AttachmentSettings.Service.EnableAttachmentPlugins)
                    {
                        Implementation.VulcanAttachmentPropertyMapper.AddMapping(media);

                        streamWriter.Write(",\"" + MediaContents + "\":{");
                        string base64contents = Convert.ToBase64String(mediaBytes);
                        long fileByteSize = mediaBytes.LongLength;

                        streamWriter.Write("\"_name\": \"" + media.Name + "\",");
                        streamWriter.Write("\"_indexed_chars\": \"-1\","); // indexes entire document instead of first 100000 chars   
                        streamWriter.Write("\"_content_type\": \"" + mimeType + "\",");
                        streamWriter.Write("\"_content_length\": \"" + fileByteSize + "\",");
                        streamWriter.Write("\"_content\": \"" + base64contents + "\"");
                        streamWriter.Write("}");
                    }

                    var converter = Helpers.GetBytesToStringConverter();
                    string stringContents = converter?.ConvertToString(mediaBytes, mimeType);

                    if (!string.IsNullOrWhiteSpace(stringContents))
                    {
                        streamWriter.Write(",\"" + MediaStringContents + "\":\"" + stringContents.JsonEscapeString() + "\"");
                    }
                }

                streamWriter.Flush();
            }
        }
    }
}