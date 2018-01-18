using EPiServer.Core;
using EPiServer.Framework.DataAnnotations;
using EPiServer.ServiceLocation;
using System;
using System.Globalization;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;
using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    /// <summary>
    /// Installs Attachments pipeline
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanPipelineInstaller), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanAttachmentPipelineInstaller : IVulcanPipelineInstaller
    {
        /// <summary>
        /// Attachment Pipeline ID
        /// </summary>
        public const string PipelineId = "vulcan-attachment";

        // 5.x support uses https://www.elastic.co/guide/en/elasticsearch/plugins/5.2/ingest-attachment.html
        // not which 2.x uses https://www.elastic.co/guide/en/elasticsearch/plugins/5.2/mapper-attachments.html
        private const bool _isVersion5 = false; // todo: nest 5 to 2 difference

        private static readonly string _pluginName = _isVersion5 ? "ingest-attachment" : "mapper-attachments"; // older 2.x";

        private readonly IVulcanAttachmentIndexerSettings _VulcanAttachmentIndexerSettings;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanAttachmentIndexerSettings"></param>
        public VulcanAttachmentPipelineInstaller(IVulcanAttachmentIndexerSettings vulcanAttachmentIndexerSettings)
        {
            _VulcanAttachmentIndexerSettings = vulcanAttachmentIndexerSettings;
        }

        string IVulcanPipelineInstaller.Id => PipelineId;

        void IVulcanPipelineInstaller.Install(IVulcanClient client)
        {
            if (_VulcanAttachmentIndexerSettings.EnableAttachmentPlugins && client.Language == CultureInfo.InvariantCulture)
            {
                var info = client.NodesInfo();

                if (info?.Nodes?.Any(x => x.Value?.Plugins?.Any(y => string.Compare(y.Name, _pluginName, true) == 0) == true) != true)
                {
                    throw new Exception($"No attachment plugin found, be sure to install the '{_pluginName}' plugin on your Elastic Search Server!");
                }

                // todo: nest 5 to 2 difference
                // v5, use pipeline
                //var response = client.PutPipeline(PipelineId, p => p
                //    .Description("Document attachment pipeline")
                //    .Processors(pr => pr
                //        .Attachment<Nest.Attachment>(a => a
                //            .Field(MediaContents)
                //            .TargetField(MediaContents)
                //            .IndexedCharacters(-1)
                //        )
                //    )
                //);
                //if (!response.IsValid)
                //{
                //    throw new Exception(response.DebugInformation);
                //}

                // v2, to do, get all MediaData types that are allowed and loop them
                var mediaDataTypes = Core.Extensions.TypeExtensions.GetSearchTypesFor<MediaData>(t => t.IsAbstract == false);

                foreach (var mediaType in mediaDataTypes)
                {
                    var descriptors = mediaType.GetCustomAttributes(false).OfType<MediaDescriptorAttribute>();
                    var extensionStrings = string.Join(",", descriptors.Select(x => x.ExtensionString ?? ""));
                    var extensions = extensionStrings.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // only map ones we allow
                    if (extensions.Union(_VulcanAttachmentIndexerSettings.SupportedFileExtensions).Any())
                    {
                        var response = client.Map<object>(m => m.
                            Index(client.IndexName). // was _all
                            Type(mediaType.FullName).
                            Properties(props => props.
                                Attachment(s => s.Name(MediaContents)
                                    .FileField(ff => ff.Name("content").Store().TermVector(Nest.TermVectorOption.WithPositionsOffsets))
                            ))
                        );

                        if (!response.IsValid)
                        {
                            throw new Exception(response.DebugInformation);
                        }
                    }
                }
            }
        }
    }
}
