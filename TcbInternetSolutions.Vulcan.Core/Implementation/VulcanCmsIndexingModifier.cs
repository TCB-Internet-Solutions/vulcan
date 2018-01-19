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
        /// <param name="args"></param>
        public virtual void ProcessContent(IVulcanIndexingModifierArgs args)
        {
            // index ancestors
            var ancestors = new List<ContentReference>();

            if (_VulcanContentAncestorLoaders?.Any() == true)
            {
                foreach (var ancestorLoader in _VulcanContentAncestorLoaders)
                {
                    IEnumerable<ContentReference> ancestorsFound = ancestorLoader.GetAncestors(args.Content);

                    if (ancestorsFound?.Any() == true)
                    {
                        ancestors.AddRange(ancestorsFound);
                    }
                }
            }

            args.AdditionalItems[VulcanFieldConstants.Ancestors] = ancestors.Select(x => x.ToReferenceWithoutVersion()).Distinct();

            // index read permission            
            var permissions = _ContentSecurityDescriptor.Get(args.Content.ContentLink);

            if (permissions != null) // will be null for commerce products, compatibility handled in commerce modifier
            {
                args.AdditionalItems[VulcanFieldConstants.ReadPermission] = permissions.Entries.
                            Where(x =>
                                x.Access.HasFlag(AccessLevel.Read) ||
                                x.Access.HasFlag(AccessLevel.Administer) ||
                                x.Access.HasFlag(AccessLevel.FullAccess)).Select(x => x.Name);
                
            }

            // index VulcanSearchableAttribute
            List<string> contents = new List<string>();
            var properties = args.Content.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(VulcanSearchableAttribute)));

            foreach (var p in properties)
            {
                object value = p.GetValue(args.Content);

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

            args.AdditionalItems[VulcanFieldConstants.CustomContents] = string.Join(" ", contents);
        }
    }
}