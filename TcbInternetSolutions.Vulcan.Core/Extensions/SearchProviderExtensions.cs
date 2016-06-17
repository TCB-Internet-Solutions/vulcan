using EPiServer;
using EPiServer.Configuration;
using EPiServer.Core;
using EPiServer.Shell;
using EPiServer.Shell.Modules;
using EPiServer.Web;
using System;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    public static class SearchProviderExtensions
    {
        private static readonly string _uriPrefix = "epi.cms.contentdata:///";

        /// <summary>
        /// Gets the full URL to edit.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="moduleTable">The module table.</param>
        internal static string GetFullUrlToEditView(this SiteDefinition settings, ModuleTable moduleTable)
        {
            string relativeUri = UriSupport.AbsolutePathForSite(VirtualPathUtilityEx.ToAppRelative(moduleTable != null ? moduleTable.ResolvePath("CMS", string.Empty) : Paths.ToResource("CMS", string.Empty)), settings);

            return new Uri(settings.SiteUrl, relativeUri).ToString();
        }

        /// <summary>
        /// Gets the URI for this instance.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>
        /// An <see cref="T:System.Uri"/> that represents the type and id of the item.
        /// </returns>
        public static Uri GetUri(this IContent content) => GetUri(content, false);

        /// <summary>
        /// Gets the URI for this instance.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="createVersionUnspecificLink">if set to <c>true</c> creates a version unspecific link.</param>
        /// <returns>
        /// An <see cref="T:System.Uri"/> that represents the type and id of the item.
        /// </returns>
        public static Uri GetUri(this IContent content, bool createVersionUnspecificLink)
        {
            ContentReference contentReference = createVersionUnspecificLink ? content.ContentLink.ToReferenceWithoutVersion() : content.ContentLink;

            return new Uri(_uriPrefix + contentReference.ToString());
        }

        /// <summary>
        /// Gets the type identifier of the content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="uiDescriptorRegistry"/>
        public static string GetTypeIdentifier(this IContent content, UIDescriptorRegistry uiDescriptorRegistry) => Enumerable.FirstOrDefault(uiDescriptorRegistry.GetTypeIdentifiers(content.GetType()));
    }
}
