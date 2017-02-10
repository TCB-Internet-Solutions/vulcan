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
    public class VulcanCmsIndexingModifier : IVulcanIndexingModifier
    {
        Injected<IContentLoader> ContentLoader;

        Injected<IVulcanHandler> VulcanHandler;

        /// <summary>
        /// Writes additional IContent information to stream
        /// </summary>
        /// <param name="content"></param>
        /// <param name="writableStream"></param>
        public virtual void ProcessContent(IContent content, Stream writableStream)
        {
            var streamWriter = new StreamWriter(writableStream);
            var ancestors = new List<ContentReference>();

            if (VulcanHandler.Service.IndexingModifers != null && VulcanHandler.Service.IndexingModifers.Any())
            {
                foreach (var indexingModifier in VulcanHandler.Service.IndexingModifers)
                {
                    IEnumerable<ContentReference> ancestorsFound = null;

                    try
                    {
                        ancestorsFound = indexingModifier.GetAncestors(content);
                    }
                    catch (NotImplementedException) { }

                    if (ancestorsFound != null && ancestorsFound.Any())
                    {
                        ancestors.AddRange(ancestorsFound);
                    }
                }
            }

            // index ancestors
            streamWriter.Write(",\"" + VulcanFieldConstants.Ancestors + "\":[");
            streamWriter.Write(string.Join(",", ancestors.Select(x => x.ToReferenceWithoutVersion()).Distinct().Select(x => "\"" + x.ToString() + "\"")));
            streamWriter.Write("]");
            
            // index read permission
            var repo = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            var permissions = repo.Get(content.ContentLink);

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

            streamWriter.Write(",\"" + VulcanFieldConstants.CustomContents + "\":\"" + string.Join(" ", contents) + "\"");

            streamWriter.Flush();
        }

        /// <summary>
        /// Gets IContent ancestors
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            return ContentLoader.Service.GetAncestors(content.ContentLink)?.Select(c => c.ContentLink);
        }
    }
}