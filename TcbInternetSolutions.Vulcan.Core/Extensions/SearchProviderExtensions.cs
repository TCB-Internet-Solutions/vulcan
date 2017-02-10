using EPiServer.Core;
using EPiServer.Editor;
using EPiServer.Shell;
using System.IO;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Search extensions
    /// </summary>
    public static class SearchProviderExtensions
    {
        /// <summary>
        /// Gets file extension for given media data.
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public static string SearchFileExtension(this MediaData media)
        {            
            if (media == null)
                return string.Empty;

            try
            {
                return Path.GetExtension(media.RouteSegment).Replace(".", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the URI for this instance.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>
        /// An <see cref="T:System.Uri"/> that represents the type and id of the item.
        /// </returns>
        public static string GetUri(this IContent content) => GetUri(content, false);

        /// <summary>
        /// Gets the URI for this instance.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="createVersionUnspecificLink">if set to <c>true</c> creates a version unspecific link.</param>
        /// <returns>
        /// An <see cref="T:System.Uri"/> that represents the type and id of the item.
        /// </returns>
        public static string GetUri(this IContent content, bool createVersionUnspecificLink)
        {
            ContentReference contentReference = createVersionUnspecificLink ? content.ContentLink.ToReferenceWithoutVersion() : content.ContentLink;

            return PageEditing.GetEditUrl(contentReference);
        }

        /// <summary>
        /// Gets the type identifier of the content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="uiDescriptorRegistry"/>
        public static string GetTypeIdentifier(this IContent content, UIDescriptorRegistry uiDescriptorRegistry) => Enumerable.FirstOrDefault(uiDescriptorRegistry.GetTypeIdentifiers(content.GetType()));
    }
}
