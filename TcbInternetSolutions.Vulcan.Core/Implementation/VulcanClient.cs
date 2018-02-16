namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using Extensions;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Principal;

    /// <summary>
    /// Default vulcan client
    /// </summary>
    public class VulcanClient : ElasticClient, IVulcanClient
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        
        // ReSharper disable once NotAccessedField.Local, needed for nest5
        private readonly IVulcanPipelineSelector _vulcanPipelineSelector;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="settings"></param>
        /// <param name="language"></param>
        /// <param name="contentLoader"></param>
        /// <param name="vulcanHandler"></param>
        /// <param name="vulcanPipelineSelector"></param>
        public VulcanClient
        (
            string index,
            IConnectionSettingsValues settings,
            CultureInfo language,
            IContentLoader contentLoader,
            IVulcanHandler vulcanHandler,
            IVulcanPipelineSelector vulcanPipelineSelector) : base(settings)
        {
            Language = language ?? throw new Exception("Vulcan client requires a language (you may use CultureInfo.InvariantCulture if needed for non-language specific data)");
            IndexName = VulcanHelper.GetIndexName(index, language);
            ContentLoader = contentLoader;
            VulcanHandler = vulcanHandler;
            _vulcanPipelineSelector = vulcanPipelineSelector;
        }

        /// <summary>
        /// Vulcan index name
        /// </summary>
        public virtual string IndexName { get; }

        /// <summary>
        /// Vulcan culture
        /// </summary>
        public virtual CultureInfo Language { get; }

        /// <summary>
        /// Injected Content Loader
        /// </summary>
        protected IContentLoader ContentLoader { get; set; }

        /// <summary>
        /// Injected Vulcan Handler
        /// </summary>
        protected IVulcanHandler VulcanHandler { get; set; }
        /// <summary>
        /// Adds a synonym
        /// </summary>
        /// <param name="term"></param>
        /// <param name="synonyms"></param>
        /// <param name="biDirectional"></param>
        public virtual void AddSynonym(string term, string[] synonyms, bool biDirectional)
        {
            VulcanHelper.AddSynonym(Language.Name, term, synonyms, biDirectional);
        }

        /// <summary>
        /// Deletes content from index
        /// </summary>
        /// <param name="content"></param>
        public virtual void DeleteContent(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null && !localizableContent.Language.Equals(Language))
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with language " + localizableContent.Language.Name + " with Vulcan client for language " + Language.GetCultureName());
            }

            if (localizableContent == null && !Language.Equals(CultureInfo.InvariantCulture))
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with no language with Vulcan client for language " + Language.Name);
            }

            try
            {
                var response = Delete(new DeleteRequest(IndexName, GetTypeName(content), GetId(content)));

                Logger.Debug("Vulcan deleted " + GetId(content) + " for language " + Language.GetCultureName() + ": " + response.DebugInformation);
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not delete content with content link " + GetId(content) + " for language " + Language.GetCultureName() + ":", e);
            }
        }

        /// <summary>
        /// Deletes content from index
        /// </summary>
        /// <param name="contentLink"></param>
        /// <param name="typeName"></param>
        public virtual void DeleteContent(ContentReference contentLink, string typeName)
        {
            // we don't know content type so try and find it in current language index
            if (string.IsNullOrWhiteSpace(typeName))
            {
                var result = SearchContent<IContent>(s => s.Query(q => q.Term(c => c.ContentLink, contentLink.ToReferenceWithoutVersion())));

                if (result != null && result.Hits.Count() >= 0)
                {
                    typeName = result.Hits.FirstOrDefault()?.Type;
                }
            }

            try
            {
                var response = Delete(new DeleteRequest(IndexName, typeName, contentLink.ToReferenceWithoutVersion().ToString()));

                Logger.Debug($"Vulcan (using direct content link) deleted {contentLink.ToReferenceWithoutVersion()} for language {Language.GetCultureName()}: {response.DebugInformation}");
            }
            catch (Exception e)
            {
                Logger.Warning($"Vulcan could not delete (using direct content link) content with content link {contentLink.ToReferenceWithoutVersion()} for language {Language.GetCultureName()}:", e);
            }

        }

        /// <summary>
        /// Gets synonyms for language
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms() => VulcanHelper.GetSynonyms(Language.Name);

        /// <summary>
        /// Index given content
        /// </summary>
        /// <param name="content"></param>
        public virtual void IndexContent(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null && !localizableContent.Language.Equals(Language))
            {
                throw new Exception($"Cannot index content '{GetId(content)}' with language {localizableContent.Language.Name} with Vulcan client for language {Language.GetCultureName()}");
            }

            if (localizableContent == null && !Language.Equals(CultureInfo.InvariantCulture))
            {
                throw new Exception($"Cannot index content '{GetId(content)}' with no language with Vulcan client for language {Language.Name}");
            }

            if (content is IVersionable versionableContent && versionableContent.Status != VersionStatus.Published) return;            
            if (!VulcanHandler.AllowContentIndexing(content)) return;

            try
            {
                var response = Index(content, ModifyContentIndexRequest);

                if (response.IsValid)
                {
                    Logger.Debug($"Vulcan indexed {GetId(content)} for language {Language.GetCultureName()}: {response.DebugInformation}");
                }
                else
                {
                    throw new Exception(response.DebugInformation);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Vulcan could not index content with content link {GetId(content)} for language {Language.GetCultureName()}: {e}");
            }
        }

        /// <summary>
        /// Remove a synonym
        /// </summary>
        /// <param name="term"></param>
        public virtual void RemoveSynonym(string term)
        {
            VulcanHelper.DeleteSynonym(Language.Name, term);
        }

        /// <summary>
        /// Search for content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchDescriptor"></param>
        /// <param name="includeNeutralLanguage"></param>
        /// <param name="rootReferences"></param>
        /// <param name="typeFilter"></param>
        /// <param name="principleReadFilter"></param>
        /// <returns></returns>
        public virtual ISearchResponse<IContent> SearchContent<T>(
                Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null,
                bool includeNeutralLanguage = false,
                IEnumerable<ContentReference> rootReferences = null,
                IEnumerable<Type> typeFilter = null,
                IPrincipal principleReadFilter = null) where T : class, IContent
        {
            var resolvedDescriptor = searchDescriptor == null ? new SearchDescriptor<T>() : searchDescriptor.Invoke(new SearchDescriptor<T>());
            typeFilter = typeFilter ?? typeof(T).GetSearchTypesFor(VulcanFieldConstants.AbstractFilter);
            resolvedDescriptor = resolvedDescriptor.Type(string.Join(",", typeFilter.Select(t => t.FullName)))
                .ConcreteTypeSelector((d, docType) => typeof(VulcanContentHit));

            var indexName = IndexName;

            if (Language.Equals(CultureInfo.InvariantCulture) && includeNeutralLanguage)
            {
                indexName += "," + VulcanHelper.GetIndexName(VulcanHandler.Index, CultureInfo.InvariantCulture);
            }

            resolvedDescriptor = resolvedDescriptor.Index(indexName);
            var validRootReferences = rootReferences?.Where(x => !ContentReference.IsNullOrEmpty(x)).ToList();
            var filters = new List<QueryContainer>();

            if (validRootReferences?.Count > 0)
            {
                var scopeDescriptor = new QueryContainerDescriptor<T>().
                    Terms(t => t.Field(VulcanFieldConstants.Ancestors).Terms(validRootReferences.Select(x => x.ToReferenceWithoutVersion().ToString())));

                filters.Add(scopeDescriptor);
            }

            if (principleReadFilter != null)
            {
                var permissionDescriptor = new QueryContainerDescriptor<T>().
                    Terms(t => t.Field(VulcanFieldConstants.ReadPermission).Terms(principleReadFilter.GetRoles()));

                filters.Add(permissionDescriptor);
            }

            if (filters.Count > 0)
            {
                var descriptor = resolvedDescriptor;
                Func<SearchDescriptor<T>, ISearchRequest> selector = ts => descriptor;
                var container = selector.Invoke(new SearchDescriptor<T>());

                if (container.Query != null)
                {
                    filters.Insert(0, container.Query);
                }

                resolvedDescriptor = resolvedDescriptor.Query(q => q.Bool(b => b.Must(filters.ToArray())));
            }

            var response = Search<T, IContent>(resolvedDescriptor);

            return response;
        }

        /// <summary>
        /// Get ID without version for content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected virtual string GetId(IContent content) => content.ContentLink.ToReferenceWithoutVersion().ToString();

        /// <summary>
        /// Gets name for content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected virtual string GetTypeName(IContent content) => content.GetTypeName();

        /// <summary>
        /// Assigns Id, Type, and Pipeline (if available) on index request
        /// </summary>
        /// <param name="indexDescriptor"></param>
        /// <returns></returns>
        protected virtual IIndexRequest ModifyContentIndexRequest(IndexDescriptor<IContent> indexDescriptor)
        {
            if (!(indexDescriptor is IIndexRequest<IContent> descriptedContent)) return null;

            var content = descriptedContent.Document;

            indexDescriptor = indexDescriptor
                .Id(GetId(content))
                .Type(GetTypeName(content));

#if NEST5
                var pipeline = _vulcanPipelineSelector.GetPipelineForContent(descriptedContent.Document);

                if (pipeline != null)
                {
                    indexDescriptor = indexDescriptor.Pipeline(pipeline.Id);
                }            
#endif

            return indexDescriptor;
        }
    }
}
