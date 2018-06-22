using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.SpecializedProperties;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// ContentArea extensions
    /// </summary>
    public static class ContentAreaExtensions
    {
        /// <summary>
        /// Converts contentarea to string for indexing
        /// </summary>
        /// <param name="contentArea"></param>
        /// <param name="contentTypeRepository"></param>
        /// <returns></returns>
        public static string GetContentAreaContents(this ContentArea contentArea, IContentTypeRepository contentTypeRepository = null)
        {
            if (contentArea == null) { return string.Empty; }

            var stringBuilder = new StringBuilder();

            foreach (var contentAreaItem in contentArea.Items)
            {
                var blockData = contentAreaItem.GetContent();
                var props = GetSearchablePropertyValues(blockData, blockData.ContentTypeID, contentTypeRepository);
                stringBuilder.AppendFormat(" {0}", string.Join(" ", props));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets searchable property values for content
        /// </summary>
        /// <param name="contentData"></param>
        /// <param name="contentType"></param>
        /// <param name="contentTypeRepository"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetSearchablePropertyValues(IContentData contentData, ContentType contentType, IContentTypeRepository contentTypeRepository)
        {
            if (contentType == null)
            {
                yield break;
            }

            foreach (var current in from d in contentType.PropertyDefinitions
                                                   where d.Searchable || typeof(IPropertyBlock).IsAssignableFrom(d.Type.DefinitionType)
                                                   select d)
            {
                var propertyData = contentData.Property[current.Name];

                if (propertyData is IPropertyBlock propertyBlock)
                {
                    foreach (var current2 in GetSearchablePropertyValues(propertyBlock.Block, propertyBlock.BlockPropertyDefinitionTypeID, contentTypeRepository))
                    {
                        yield return current2;
                    }
                }
                else
                {
                    yield return propertyData.ToWebString();
                }
            }
        }

        /// <summary>
        /// Gets searchable propety values for content and type Id
        /// </summary>
        /// <param name="contentData"></param>
        /// <param name="contentTypeId"></param>
        /// <param name="contentTypeRepository"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetSearchablePropertyValues(IContentData contentData, int contentTypeId, IContentTypeRepository contentTypeRepository) =>
            GetSearchablePropertyValues(contentData, ResolveContentTypeRepository(contentTypeRepository).Load(contentTypeId), contentTypeRepository);

        private static IContentTypeRepository ResolveContentTypeRepository(IContentTypeRepository contentTypeRepository)
        {
            return contentTypeRepository ?? VulcanHelper.GetService<IContentTypeRepository>();
        }
    }
}
