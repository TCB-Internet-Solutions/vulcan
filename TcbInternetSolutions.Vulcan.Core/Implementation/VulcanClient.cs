using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanClient : ElasticClient, IVulcanClient
    {
        private static ILogger Logger = LogManager.GetLogger();

        public Injected<IContentLoader> ContentLoader { get; set; }
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public CultureInfo Language { get; }

        public string IndexName { get; }

        public VulcanClient(string index, ConnectionSettings settings, CultureInfo language)
            : base(settings)
        {
            if (language == null)
            {
                throw new Exception("Vulcan client requires a language (you may use CultureInfo.InvariantCulture if needed for non-language specific data)");
            }

            Language = language;
            IndexName = VulcanHelper.GetIndexName(index, Language);
        }

        public ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null, bool includeNeutralLanguage = false, IEnumerable<ContentReference> rootReferences = null) where T : class, EPiServer.Core.IContent
        {
            SearchDescriptor<T> resolvedDescriptor;

            if (searchDescriptor == null)
            {
                resolvedDescriptor = new SearchDescriptor<T>();
            }
            else
            {
                resolvedDescriptor = searchDescriptor.Invoke(new SearchDescriptor<T>());
            }

            var types = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t) && !t.FullName.EndsWith("Proxy")).Select(t => t.FullName));
            }

            resolvedDescriptor = resolvedDescriptor.Type(string.Join(",", types)).ConcreteTypeSelector((d, docType) => typeof(VulcanContentHit));

            var indexName = IndexName;
            if (Language != CultureInfo.InvariantCulture && includeNeutralLanguage)
            {
                indexName += "," + VulcanHelper.GetIndexName(VulcanHandler.Service.Index, CultureInfo.InvariantCulture);
            }

            resolvedDescriptor = resolvedDescriptor.Index(indexName);
            var validRootReferences = rootReferences?.Where(x => !ContentReference.IsNullOrEmpty(x)).ToList();

            if (validRootReferences.Count > 0)
            {
                Func<SearchDescriptor<T>, ISearchRequest> selector = ts => resolvedDescriptor;
                var container = selector.Invoke(new SearchDescriptor<T>());
                var blendDescriptor = new QueryContainerDescriptor<T>();

                // TODO: Figure out how to support multiple roots with OR

                blendDescriptor = blendDescriptor.Term(t => t
                    .Field(VulcanFieldConstants.Ancestors)
                        .Value(validRootReferences.FirstOrDefault().ToReferenceWithoutVersion().ToString())) as QueryContainerDescriptor<T>;

                if (container.Query != null)
                {
                    resolvedDescriptor = resolvedDescriptor.Query(q => q
                        .Bool(b => b
                            .Must(new QueryContainer[] { container.Query, blendDescriptor })));
                }
                else
                {
                    resolvedDescriptor = resolvedDescriptor.Query(q => q
                        .Bool(b => b
                            .Must(new QueryContainer[] { blendDescriptor })));
                }
            }

            return base.Search<T, IContent>(resolvedDescriptor);
        }

        public void IndexContent(IContent content)
        {
            if (content is ILocalizable && (content as ILocalizable).Language != Language)
            {
                throw new Exception("Cannot index content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name));
            }

            if (!(content is ILocalizable) && Language != CultureInfo.InvariantCulture)
            {
                throw new Exception("Cannot index content '" + GetId(content) + "' with no language with Vulcan client for language " + Language.Name);
            }

            if (!(content is IVersionable) || (content as IVersionable).Status == VersionStatus.Published)
            {
                try
                {
                    var response = base.Index(content, c => c.Id(GetId(content)).Type(GetTypeName(content)));

                    if (response.IsValid)
                    {
                        Logger.Debug("Vulcan indexed " + GetId(content) + " for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name) + ": " + response.DebugInformation);
                    }
                    else
                    {
                        throw new Exception(response.DebugInformation);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Vulcan could not index content with content link " + GetId(content) + " for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name) + ": ", e);
                }
            }
        }

        public void DeleteContent(IContent content)
        {
            if (content is ILocalizable && (content as ILocalizable).Language != Language)
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name));
            }

            if (!(content is ILocalizable) && Language != CultureInfo.InvariantCulture)
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with no language with Vulcan client for language " + Language.Name);
            }

            try
            {
                var response = base.Delete(new DeleteRequest(IndexName, GetTypeName(content), GetId(content)));

                Logger.Debug("Vulcan deleted " + GetId(content) + " for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name) + ": " + response.DebugInformation);
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not delete content with content link " + GetId(content) + " for language " + (Language == CultureInfo.InvariantCulture ? "invariant" : Language.Name) + ":", e);
            }
        }

        private string GetTypeName(IContent content) => content.GetType().Name.EndsWith("Proxy") ? content.GetType().BaseType.FullName : content.GetType().FullName;

        private string GetId(IContent content) => content.ContentLink.ToReferenceWithoutVersion().ToString();

        public void AddSynonym(string term, string[] synonyms, bool biDirectional)
        {
            VulcanHelper.AddSynonym(Language.Name, term, synonyms, biDirectional);
        }

        public void RemoveSynonym(string term)
        {
            VulcanHelper.DeleteSynonym(Language.Name, term);
        }

        public Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms() => VulcanHelper.GetSynonyms(Language.Name);
    }
}
