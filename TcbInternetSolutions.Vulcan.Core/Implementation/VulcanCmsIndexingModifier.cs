namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
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
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifierWithAncestors
    {
        private readonly IContentLoader _ContentLoader;
        private readonly IContentSecurityRepository _ContentSecurityDescriptor;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentLoader"></param>
        /// <param name="contentSecurityRepository"></param>
        public VulcanCmsIndexingModifier(IContentLoader contentLoader, IContentSecurityRepository contentSecurityRepository)
        {
            _ContentLoader = contentLoader;            
            _ContentSecurityDescriptor = contentSecurityRepository;
        }

        /// <summary>
        /// Writes additional IContent information to stream
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        public virtual void ProcessContent(IContent content, Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);
            var ancestors = new List<ContentReference>();

            //hack: to avoid a circular dependency by injecting an IVulcanHandler, get it from service locator here
            // to fix remove IEnumerable<IVulcanIndexingModifier> IndexingModifers { get; } from IVulcanHandler
            var vulcanHandler = ServiceLocator.Current.GetInstance<IVulcanHandler>();

            if (vulcanHandler.IndexingModifers?.Any() == true)
            {
                foreach (var indexingModifier in vulcanHandler.IndexingModifers)
                {
                    if (indexingModifier is IVulcanIndexingModifierWithAncestors modifierWithAncestors)
                    {
                        IEnumerable<ContentReference> ancestorsFound = modifierWithAncestors.GetAncestors(content);

                        if (ancestorsFound?.Any() == true)
                        {
                            ancestors.AddRange(ancestorsFound);
                        }

                    }
                }
            }

            // index ancestors
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

        /// <summary>
        /// Gets IContent ancestors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            return _ContentLoader.GetAncestors(content.ContentLink)?.Select(c => c.ContentLink);
        }
    }
}