namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using System;
    using TcbInternetSolutions.Vulcan.Core;
    using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

    /// <summary>
    /// Adds attachment content to serialized data
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanIndexingModifier), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentIndexModifier : IVulcanIndexingModifier
    {
        private readonly IVulcanAttachmentInspector _AttachmentInspector;
        private readonly IVulcanMediaReader _MediaReader;
        private readonly IVulcanBytesToStringConverter _ByteConvertor;
        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexModifier));

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanAttachmentInspector"></param>
        /// <param name="vulcanMediaReader"></param>
        /// <param name="vulcanBytesToStringConverter"></param>
        public VulcanAttachmentIndexModifier
        (
            IVulcanAttachmentInspector vulcanAttachmentInspector,
            IVulcanMediaReader vulcanMediaReader,
            IVulcanBytesToStringConverter vulcanBytesToStringConverter
        )
        {
            _AttachmentInspector = vulcanAttachmentInspector;
            _MediaReader = vulcanMediaReader;
            _ByteConvertor = vulcanBytesToStringConverter;
        }

        /// <summary>
        /// Adds attachment content to serialized data
        /// </summary>
        /// <param name="args"></param>
        public void ProcessContent(IVulcanIndexingModifierArgs args)//, Stream writableStream)
        {
            if (args.Content is MediaData media)
            {
                //var streamWriter = new StreamWriter(writableStream);
                byte[] mediaBytes = _MediaReader.ReadToEnd(media);
                string mimeType = media.MimeType;

                // only send base64 content if pipeline is enabled
                if (args.PipelineId == Implementation.VulcanAttachmentPipelineInstaller.PipelineId)
                {
                    string base64contents = Convert.ToBase64String(mediaBytes);
                    args.AdditionalItems[MediaContents] = base64contents;
                    //streamWriter.Write(",\"" + MediaContents + "\" : \"" + base64contents + "\"");

                    // v2x
                    //streamWriter.Write(",\"" + MediaContents + "\":{");                    
                    //long fileByteSize = mediaBytes.LongLength;

                    //streamWriter.Write("\"_name\": \"" + media.Name + "\",");
                    //streamWriter.Write("\"_indexed_chars\": \"-1\","); // indexes entire document instead of first 100000 chars   
                    //streamWriter.Write("\"_content_type\": \"" + mimeType + "\",");
                    //streamWriter.Write("\"_content_length\": \"" + fileByteSize + "\",");
                    //streamWriter.Write("\"_content\": \"" + base64contents + "\"");
                    //streamWriter.Write("}");
                }

                string stringContents = _ByteConvertor.ConvertToString(mediaBytes, mimeType);

                if (!string.IsNullOrWhiteSpace(stringContents))
                {
                    args.AdditionalItems[MediaStringContents] = stringContents;
                    //streamWriter.Write(",\"" + MediaStringContents + "\": " + stringContents.JsonEscapeString()); // json escape adds quotes
                }

                //streamWriter.Flush();
            }
        }
    }
}