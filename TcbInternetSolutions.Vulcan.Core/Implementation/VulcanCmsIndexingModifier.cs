namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using TcbInternetSolutions.Vulcan.Core.Extensions;

    /// <summary>
    /// Default CMS content indexing modifier
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanIndexingModifier), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {        
        private readonly IContentSecurityRepository _ContentSecurityDescriptor;
        private readonly IEnumerable<IVulcanContentAncestorLoader> _VulcanContentAncestorLoaders;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentSecurityRepository"></param>
        /// <param name="vulcanContentAncestorLoader"></param>
        public VulcanCmsIndexingModifier(IContentSecurityRepository contentSecurityRepository, IEnumerable<IVulcanContentAncestorLoader> vulcanContentAncestorLoader)
        {
            _ContentSecurityDescriptor = contentSecurityRepository;
            _VulcanContentAncestorLoaders = vulcanContentAncestorLoader;
        }

        /// <summary>
        /// Writes additional IContent information to stream
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        public virtual void ProcessContent(IContent content, Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);

            // index ancestors
            var ancestors = new List<ContentReference>();

            if (_VulcanContentAncestorLoaders?.Any() == true)
            {
                foreach (var ancestorLoader in _VulcanContentAncestorLoaders)
                {
                    IEnumerable<ContentReference> ancestorsFound = ancestorLoader.GetAncestors(content);

                    if (ancestorsFound?.Any() == true)
                    {
                        ancestors.AddRange(ancestorsFound);
                    }
                }
            }

            streamWriter.Write(",\"" + VulcanFieldConstants.Ancestors + "\":[");
            streamWriter.Write(string.Join(",", ancestors.Select(x => x.ToReferenceWithoutVersion()).Distinct().Select(x => "\"" + x.ToString() + "\"")));
            streamWriter.Write("]");

            // index read permission            
            var permissions = _ContentSecurityDescriptor.Get(content.ContentLink);

            if (permissions != null) // will be null for commerce products, compatibility handled in commerce modifier
            {
                streamWriter.Write(",\"" + VulcanFieldConstants.ReadPermission + "\":[");
                streamWriter.Write(string.Join(",", permissions.Entries.
                            Where(x =>
                                x.Access.HasFlag(AccessLevel.Read) ||
                                x.Access.HasFlag(AccessLevel.Administer) ||
                                x.Access.HasFlag(AccessLevel.FullAccess))
                            .Select(x => StringExtensions.JsonEscapeString(x.Name)) // json escape adds quotes
                        ));
                streamWriter.Write("]");
            }

            // index VulcanSearchableAttribute
            List<string> contents = new List<string>();
            var properties = content.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(VulcanSearchableAttribute)));

            foreach (var p in properties)
            {
                object value = p.GetValue(content);

                // Property to string conversions
                if (p.PropertyType == typeof(ContentArea))
                {
                    value = ContentAreaExtensions.GetContentAreaContents(value as ContentArea);
                }

                string v = value?.ToString();

                if (!string.IsNullOrWhiteSpace(v))
                {
                    contents.Add(v);
                }
            }

            streamWriter.Write(",\"" + VulcanFieldConstants.CustomContents + "\":" + StringExtensions.JsonEscapeString(string.Join(" ", contents)));

            streamWriter.Flush();
        }
    }
}