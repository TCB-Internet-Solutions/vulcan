namespace TcbInternetSolutions.Vulcan.AttachmentIndexer
{
    using EPiServer.Core;
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Implementation;
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

        private Type _converterType;

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
            if (!IsMediaReadable(args, out var isPipeline, out var media)) return;
            var mediaBytes = _mediaReader.ReadToEnd(media);
            var base64Contents = Convert.ToBase64String(mediaBytes);
            var mimeType = media.MimeType;

            var stringContents = _byteConvertor.ConvertToString(mediaBytes, mimeType);

            if (!string.IsNullOrWhiteSpace(stringContents))
            {
                args.AdditionalItems[MediaStringContents] = stringContents;
            }

            if (!isPipeline) return;

#if NEST2
            var mediaFields = new Dictionary<string, object>
            {
                ["_name"] = media.Name,
                ["_indexed_chars"] = -1,// indexes entire document instead of first 100000 chars   
                ["_content_type"] = mimeType,
                ["_content_length"] = mediaBytes.LongLength,
                ["_content"] = base64Contents
            };

            args.AdditionalItems[MediaContents] = mediaFields;
#elif NEST5
            // 5x: only send base64 content if pipeline is enabled
            args.AdditionalItems[MediaContents] = base64Contents;
#endif
        }

        private bool IsMediaReadable(IVulcanIndexingModifierArgs args, out bool isPipeline, out MediaData media)
        {
            media = args.Content as MediaData;
            isPipeline = false;

            if (media == null) return false;

            if (_attachmentPipeline == null)
            {
                _attachmentPipeline = _vulcanPipelineSelector.GetPipelineById(Implementation.VulcanAttachmentPipelineInstaller.PipelineId);
            }

#if NEST2
            // for 2x, have to evaluate pipeline here
            if (_attachmentPipeline?.IsMatch(args.Content) == true)
            {
                return isPipeline = true;
            }
#endif

            if (args.PipelineId == Implementation.VulcanAttachmentPipelineInstaller.PipelineId)
            {
                return isPipeline = true;
            }

            if (_converterType == null)
            {
                _converterType = _byteConvertor.GetType();
            }            

            // default converter does nothing so don't read it
            return _converterType != typeof(DefaultVulcanBytesToStringConverter);
        }
    }
}