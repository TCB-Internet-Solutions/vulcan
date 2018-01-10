using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Globalization;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.Core.Extensions;
using static TcbInternetSolutions.Vulcan.Core.VulcanFieldConstants;

namespace TcbInternetSolutions.Vulcan.AttachmentIndexer.Implementation
{
    /// <summary>
    /// Adds property mapping for attachments
    /// </summary>
    public class VulcanAttachmentPropertyMapper
    {
        private static ILogger _Logger = LogManager.GetLogger(typeof(VulcanAttachmentPropertyMapper));

        internal static List<string> AddedMappings = new List<string>();

        /// <summary>
        /// Add mapping via IContent
        /// </summary>
        /// <param name="mediaType"></param>
        public static void AddMapping(IContent mediaType) => AddMapping(mediaType.GetTypeName());

        /// <summary>
        /// Add mapping by Type
        /// </summary>
        /// <param name="mediaType"></param>
        public static void AddMapping(Type mediaType) => AddMapping(mediaType.FullName);

        /// <summary>
        /// Add mapping by string
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="vulcanHandler"></param>
        public static void AddMapping(string typeName, IVulcanHandler vulcanHandler = null)
        {
            if (AddedMappings?.Contains(typeName) == true) return;

            vulcanHandler = vulcanHandler ?? ServiceLocator.Current.GetInstance<IVulcanHandler>(); // fallback if not set

            try
            {   
                IVulcanClient client = vulcanHandler.GetClient(CultureInfo.InvariantCulture);

                var response = client.PutPipeline("attachment", p => p
                        .Description("Document attachment pipeline")
                        .Processors(pr => pr
                        .Attachment<Nest.Attachment>(a => a
                            .Field(MediaContents)
                            .TargetField(MediaContents)
                            .IndexedCharacters(-1)
                    )));
                    //.Processors(pp => pp
                    //    .Foreach<object>(fe => fe
                    //        //.Field(MediaContents)
                    //        .Processor(fep => fep
                    //            .Attachment<Nest.Attachment>(a => a.Field(MediaContents))
                    //        )
                    //    )));

                //var response = client.Map<object>(m => m.
                //    Index("_all").
                //    Type(typeName).
                //        Properties(props => props.
                //            Attachment(s => s.Name(MediaContents)                                
                //                .FileField(ff => ff.Name("content").Store().TermVector(Nest.TermVectorOption.WithPositionsOffsets))
                //                )));

                if (!response.IsValid)
                {
                    throw new Exception(response.DebugInformation);
                }

                AddedMappings.Add(typeName);
            }
            catch (Exception ex)
            {
                _Logger.Error("Failed to map attachment field for type: " + typeName, ex);
            }
        }
    }
}
