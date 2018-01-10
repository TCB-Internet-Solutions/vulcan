namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using Nest;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Principal;
    using TcbInternetSolutions.Vulcan.Core.Extensions;

    /// <summary>
    /// Default vulcan client
    /// </summary>
    public class VulcanClient : ElasticClient, IVulcanClient
    {
        private static ILogger Logger = LogManager.GetLogger();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="settings"></param>
        /// <param name="language"></param>
        public VulcanClient(string index, ConnectionSettings settings, CultureInfo language) : 
            this(index, settings, language, ServiceLocator.Current.GetInstance<IContentLoader>(), ServiceLocator.Current.GetInstance<IVulcanHandler>())            
        { }

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="settings"></param>
        /// <param name="language"></param>
        /// <param name="contentLoader"></param>
        /// <param name="vulcanHandler"></param>
        public VulcanClient(string index, ConnectionSettings settings, CultureInfo language, IContentLoader contentLoader, IVulcanHandler vulcanHandler) : base(settings)
        {
            Language = language ?? throw new Exception("Vulcan client requires a language (you may use CultureInfo.InvariantCulture if needed for non-language specific data)");
            IndexName = VulcanHelper.GetIndexName(index, Language);
            ContentLoader = contentLoader;
            VulcanHandler = vulcanHandler;
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
                throw new Exception("Cannot delete content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + Language.GetCultureName());
            }

            if (localizableContent == null && !Language.Equals(CultureInfo.InvariantCulture))
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with no language with Vulcan client for language " + Language.Name);
            }

            try
            {
                var response = base.Delete(new DeleteRequest(IndexName, GetTypeName(content), GetId(content)));

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
        public virtual void DeleteContent(ContentReference contentLink)
        {
            // we don't know content type so try and find it in current language index

            var result = SearchContent<IContent>(s => s.Query(q => q.Term(c => c.ContentLink, contentLink.ToReferenceWithoutVersion())));
            
            if (result != null && result.Hits.Count() >= 0)
            {
                try
                {
                    var response = base.Delete(new DeleteRequest(IndexName, result.Hits.First().Type, contentLink.ToReferenceWithoutVersion().ToString()));

                    Logger.Debug("Vulcan (using direct content link) deleted " + contentLink.ToReferenceWithoutVersion().ToString() + " for language " + Language.GetCultureName() + ": " + response.DebugInformation);
                }
                catch (Exception e)
                {
                    Logger.Warning("Vulcan could not delete (using direct content link) content with content link " + contentLink.ToReferenceWithoutVersion().ToString() + " for language " + Language.GetCultureName() + ":", e);
                }
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
                throw new Exception("Cannot index content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + Language.GetCultureName());
            }

            if (localizableContent == null && !Language.Equals(CultureInfo.InvariantCulture))
            {
                throw new Exception("Cannot index content '" + GetId(content) + "' with no language with Vulcan client for language " + Language.Name);
            }

            var versionableContent = content as IVersionable;

            if (versionableContent == null || versionableContent.Status == VersionStatus.Published)
            {
                // see if we should index this content
                if (VulcanHandler.AllowContentIndexing(content))
                {
                    try
                    {
                        // todo: need to conditionally enable pipeline when its an indexable type
                        var response = base.Index(content, c => c.Id(GetId(content)).Type(GetTypeName(content)));//.Pipeline("attachment"));

                        if (response.IsValid)
                        {
                            Logger.Debug("Vulcan indexed " + GetId(content) + " for language " + Language.GetCultureName() + ": " + response.DebugInformation);
                        }
                        else
                        {
                            throw new Exception(response.DebugInformation);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Vulcan could not index content with content link " + GetId(content) + " for language " + Language.GetCultureName() + ": ", e);
                    }
                }
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
            SearchDescriptor<T> resolvedDescriptor;

            if (searchDescriptor == null)
            {
                resolvedDescriptor = new SearchDescriptor<T>();
            }
            else
            {
                resolvedDescriptor = searchDescriptor.Invoke(new SearchDescriptor<T>());
            }

            typeFilter = typeFilter ?? typeof(T).GetSearchTypesFor(VulcanFieldConstants.AbstractFilter);
            resolvedDescriptor = resolvedDescriptor.Type(string.Join(",", typeFilter.Select(t => t.FullName)))
                .ConcreteTypeSelector((d, docType) => typeof(VulcanContentHit));

            var indexName = IndexName;

            if (Language != CultureInfo.InvariantCulture && includeNeutralLanguage)
            {
                indexName += "," + VulcanHelper.GetIndexName(VulcanHandler.Index, CultureInfo.InvariantCulture);
            }

            resolvedDescriptor = resolvedDescriptor.Index(indexName);
            var validRootReferences = rootReferences?.Where(x => !ContentReference.IsNullOrEmpty(x)).ToList();
            List<QueryContainer> filters = new List<QueryContainer>();

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
                Func<SearchDescriptor<T>, ISearchRequest> selector = ts => resolvedDescriptor;
                var container = selector.Invoke(new SearchDescriptor<T>());

                if (container.Query != null)
                {
                    filters.Insert(0, container.Query);
                }

                resolvedDescriptor = resolvedDescriptor.Query(q => q.Bool(b => b.Must(filters.ToArray())));
            }

            var response = base.Search<T, IContent>(resolvedDescriptor);

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
    }
}
