namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Logging;
    using System;
    using System.IO;
    using TcbInternetSolutions.Vulcan.Core.Extensions;
    using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

    /// <summary>
    /// Adds attachment content to serialized data
    /// </summary>
    public class VulcanAttachmentIndexModifier : Core.IVulcanIndexingModifier
    {
        private readonly IVulcanAttachmentInspector _AttachmentInspector;
        private readonly IVulcanAttachmentIndexerSettings _AttachmentSettings;
        private readonly IVulcanMediaReader _MediaReader;
        private readonly IVulcanBytesToStringConverter _ByteConvertor;
        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexModifier));

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanAttachmentInspector"></param>
        /// <param name="vulcanAttachmentIndexerSettings"></param>
        /// <param name="vulcanMediaReader"></param>
        /// <param name="vulcanBytesToStringConverter"></param>
        public VulcanAttachmentIndexModifier
            (
                IVulcanAttachmentInspector vulcanAttachmentInspector,
                IVulcanAttachmentIndexerSettings vulcanAttachmentIndexerSettings,
                IVulcanMediaReader vulcanMediaReader,
                IVulcanBytesToStringConverter vulcanBytesToStringConverter
            )
        {
            _AttachmentInspector = vulcanAttachmentInspector;
            _AttachmentSettings = vulcanAttachmentIndexerSettings;
            _MediaReader = vulcanMediaReader;
            _ByteConvertor = vulcanBytesToStringConverter;
        }
        
        /// <summary>
        /// Adds attachment content to serialized data
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        public void ProcessContent(IContent content, Stream writableStream)
        {
            if (content is MediaData media && _AttachmentInspector.AllowIndexing(media))
            {
                var streamWriter = new StreamWriter(writableStream);
                byte[] mediaBytes = _MediaReader.ReadToEnd(media);
                string mimeType = media.MimeType;
                long fileByteSize = mediaBytes?.LongLength ?? 0;

                if (fileByteSize > 0)
                {
                    if (_AttachmentSettings.EnableAttachmentPlugins)
                    {
                        Implementation.VulcanAttachmentPropertyMapper.AddMapping(media);
                        string base64contents = Convert.ToBase64String(mediaBytes);

                        streamWriter.Write(",\"" + MediaContents + "\" : \"" + base64contents + "\"");

                        //streamWriter.Write(",\"" + MediaContents + "\":{");
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
                        streamWriter.Write(",\"" + MediaStringContents + "\": " + stringContents.JsonEscapeString()); // json escape adds quotes
                    }
                }

                streamWriter.Flush();
            }
        }
    }
}