using EPiServer.Core;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Vulcan client contract
    /// </summary>
    public interface IVulcanClient : IElasticClient
    {
        /// <summary>
        /// Search content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchDescriptor"></param>
        /// <param name="includeNeutralLanguage"></param>
        /// <param name="rootReferences"></param>
        /// <param name="typeFilter"></param>
        /// <param name="principleReadFilter"></param>
        /// <returns></returns>
        ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null,
                bool includeNeutralLanguage = false,
                IEnumerable<ContentReference> rootReferences = null,
                IEnumerable<Type> typeFilter = null,
                IPrincipal principleReadFilter = null) where T : class, IContent;

        /// <summary>
        /// Index content
        /// </summary>
        /// <param name="content"></param>
        void IndexContent(IContent content);

        /// <summary>
        /// Delete content
        /// </summary>
        /// <param name="content"></param>
        void DeleteContent(IContent content);

        /// <summary>
        /// Delete content
        /// </summary>
        /// <param name="contentLink"></param>
        /// <param name="typeName"></param>
        void DeleteContent(ContentReference contentLink, string typeName);

        /// <summary>
        /// Index name
        /// </summary>
        string IndexName { get; }

        /// <summary>
        /// client language
        /// </summary>
        CultureInfo Language { get; }

        /// <summary>
        /// Add synonym
        /// </summary>
        /// <param name="term"></param>
        /// <param name="synonyms"></param>
        /// <param name="biDirectional"></param>
        void AddSynonym(string term, string [] synonyms, bool biDirectional);

        /// <summary>
        /// Delete synonym
        /// </summary>
        /// <param name="term"></param>
        void RemoveSynonym(string term);

        /// <summary>
        /// Get synonyms
        /// </summary>
        /// <returns></returns>
        Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms();
    }
}
