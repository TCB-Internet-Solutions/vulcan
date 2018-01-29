namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Security;
    using EPiServer.ServiceLocation;
    using Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Default CMS content indexing modifier
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanIndexingModifier), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {        
        private readonly IContentSecurityRepository _contentSecurityDescriptor;
        private readonly IEnumerable<IVulcanContentAncestorLoader> _vulcanContentAncestorLoaders;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentSecurityRepository"></param>
        /// <param name="vulcanContentAncestorLoader"></param>
        public VulcanCmsIndexingModifier(IContentSecurityRepository contentSecurityRepository, IEnumerable<IVulcanContentAncestorLoader> vulcanContentAncestorLoader)
        {
            _contentSecurityDescriptor = contentSecurityRepository;
            _vulcanContentAncestorLoaders = vulcanContentAncestorLoader;
        }

        /// <summary>
        /// Writes additional IContent information to stream
        /// </summary>
        /// <param name="args"></param>
        public virtual void ProcessContent(IVulcanIndexingModifierArgs args)
        {
            // index ancestors
            var ancestors = new List<ContentReference>();

            if (_vulcanContentAncestorLoaders?.Any() == true)
            {
                foreach (var ancestorLoader in _vulcanContentAncestorLoaders)
                {
                    var ancestorsFound = ancestorLoader.GetAncestors(args.Content)?.ToList();

                    if (ancestorsFound?.Any() == true)
                    {
                        ancestors.AddRange(ancestorsFound);
                    }
                }
            }

            args.AdditionalItems[VulcanFieldConstants.Ancestors] = ancestors.Select(x => x.ToReferenceWithoutVersion()).Distinct();

            // index read permission            
            var permissions = _contentSecurityDescriptor.Get(args.Content.ContentLink);

            if (permissions != null) // will be null for commerce products, compatibility handled in commerce modifier
            {
                args.AdditionalItems[VulcanFieldConstants.ReadPermission] = permissions.Entries.
                            Where(x =>
                                x.Access.HasFlag(AccessLevel.Read) ||
                                x.Access.HasFlag(AccessLevel.Administer) ||
                                x.Access.HasFlag(AccessLevel.FullAccess)).Select(x => x.Name);
                
            }

            // index VulcanSearchableAttribute
            var contents = new List<string>();
            var properties = args.Content.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(VulcanSearchableAttribute)));

            foreach (var p in properties)
            {
                var value = p.GetValue(args.Content);

                // Property to string conversions
                if (p.PropertyType == typeof(ContentArea))
                {
                    value = (value as ContentArea).GetContentAreaContents();
                }

                var v = value?.ToString();

                if (!string.IsNullOrWhiteSpace(v))
                {
                    contents.Add(v);
                }
            }

            args.AdditionalItems[VulcanFieldConstants.CustomContents] = string.Join(" ", contents);
        }
    }
}