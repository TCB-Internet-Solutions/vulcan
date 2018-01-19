namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
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
        private readonly IVulcanPipelineSelector _VulcanPipelineSelector;
        private ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentIndexModifier));

        // store the attachment pipeline for NEST 2 since its a singleton and no need to get it for every asset
        private IVulcanPipeline _AttachmentPipeline;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanAttachmentInspector"></param>
        /// <param name="vulcanMediaReader"></param>
        /// <param name="vulcanBytesToStringConverter"></param>
        /// <param name="vulcanPipelineSelector"></param>
        public VulcanAttachmentIndexModifier
        (
            IVulcanAttachmentInspector vulcanAttachmentInspector,
            IVulcanMediaReader vulcanMediaReader,
            IVulcanBytesToStringConverter vulcanBytesToStringConverter,
            IVulcanPipelineSelector vulcanPipelineSelector
        )
        {
            _AttachmentInspector = vulcanAttachmentInspector;
            _MediaReader = vulcanMediaReader;
            _ByteConvertor = vulcanBytesToStringConverter;
            _VulcanPipelineSelector = vulcanPipelineSelector;
        }

        /// <summary>
        /// Adds attachment content to serialized data
        /// </summary>
        /// <param name="args"></param>
        public void ProcessContent(IVulcanIndexingModifierArgs args)
        {
            if (args.Content is MediaData media)
            {
                byte[] mediaBytes = _MediaReader.ReadToEnd(media);
                string mimeType = media.MimeType;

#if NEST2
                // for 2x, have to evaluate pipeline here
                if (_AttachmentPipeline == null)
                {
                    _AttachmentPipeline = _VulcanPipelineSelector.GetPipelineById(Implementation.VulcanAttachmentPipelineInstaller.PipelineId);
                }

                if (_AttachmentPipeline?.IsMatch(args.Content) == true)
                {
                    string base64contents = Convert.ToBase64String(mediaBytes);
                    Dictionary<string, object> mediaFields = new Dictionary<string, object>
                    {
                        ["_name"] = media.Name,
                        ["_indexed_chars"] = -1,// indexes entire document instead of first 100000 chars   
                        ["_content_type"] = mimeType,
                        ["_content_length"] = mediaBytes.LongLength,
                        ["_content"] = base64contents
                    };

                    args.AdditionalItems[MediaContents] = mediaFields;
                }
#elif NEST5
                // 5x: only send base64 content if pipeline is enabled
                if (args.PipelineId == Implementation.VulcanAttachmentPipelineInstaller.PipelineId)
                {
                    string base64contents = Convert.ToBase64String(mediaBytes);

                    args.AdditionalItems[MediaContents] = base64contents;
                }
#endif
                string stringContents = _ByteConvertor.ConvertToString(mediaBytes, mimeType);

                if (!string.IsNullOrWhiteSpace(stringContents))
                {
                    args.AdditionalItems[MediaStringContents] = stringContents;
                }
            }
        }
    }
}