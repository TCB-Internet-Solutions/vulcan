namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using Core;
    using static Core.VulcanFieldConstants;

    /// <summary>
    /// Adds attachment content to serialized data
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanIndexingModifier), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentIndexModifier : IVulcanIndexingModifier
    {        
        private readonly IVulcanMediaReader _mediaReader;
        private readonly IVulcanBytesToStringConverter _byteConvertor;
        private readonly IVulcanPipelineSelector _vulcanPipelineSelector;

        // store the attachment pipeline for NEST 2 since its a singleton and no need to get it for every asset
        private IVulcanPipeline _attachmentPipeline;

        /// <summary>
        /// DI Constructor
        /// </summary>        
        /// <param name="vulcanMediaReader"></param>
        /// <param name="vulcanBytesToStringConverter"></param>
        /// <param name="vulcanPipelineSelector"></param>
        public VulcanAttachmentIndexModifier
        (            
            IVulcanMediaReader vulcanMediaReader,
            IVulcanBytesToStringConverter vulcanBytesToStringConverter,
            IVulcanPipelineSelector vulcanPipelineSelector
        )
        {            
            _mediaReader = vulcanMediaReader;
            _byteConvertor = vulcanBytesToStringConverter;
            _vulcanPipelineSelector = vulcanPipelineSelector;
        }

        /// <summary>
        /// Adds attachment content to serialized data
        /// </summary>
        /// <param name="args"></param>
        public void ProcessContent(IVulcanIndexingModifierArgs args)
        {
            if (!(args.Content is MediaData media)) return;
            var mediaBytes = _mediaReader.ReadToEnd(media);
            var mimeType = media.MimeType;

#if NEST2
            // for 2x, have to evaluate pipeline here
            if (_attachmentPipeline == null)
            {
                _attachmentPipeline = _vulcanPipelineSelector.GetPipelineById(Implementation.VulcanAttachmentPipelineInstaller.PipelineId);
            }

            if (_attachmentPipeline?.IsMatch(args.Content) == true)
            {
                var base64Contents = Convert.ToBase64String(mediaBytes);
                var mediaFields = new Dictionary<string, object>
                {
                    ["_name"] = media.Name,
                    ["_indexed_chars"] = -1,// indexes entire document instead of first 100000 chars   
                    ["_content_type"] = mimeType,
                    ["_content_length"] = mediaBytes.LongLength,
                    ["_content"] = base64Contents
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
            var stringContents = _byteConvertor.ConvertToString(mediaBytes, mimeType);

            if (!string.IsNullOrWhiteSpace(stringContents))
            {
                args.AdditionalItems[MediaStringContents] = stringContents;
            }
        }
    }
}