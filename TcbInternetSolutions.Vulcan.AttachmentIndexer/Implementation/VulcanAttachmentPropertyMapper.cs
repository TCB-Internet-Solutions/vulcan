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
        public static void AddMapping(string typeName)
        {
            if (AddedMappings?.Contains(typeName) == true) return;

            try
            {   
                IVulcanClient client = ServiceLocator.Current.GetInstance<IVulcanHandler>().GetClient(CultureInfo.InvariantCulture);
                var response = client.Map<object>(m => m.
                    Index("_all").
                    Type(typeName).
                        Properties(props => props.
                            Attachment(s => s.Name(MediaContents)                                
                                .FileField(ff => ff.Name("content").Store().TermVector(Nest.TermVectorOption.WithPositionsOffsets))
                                ))
                    );

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
