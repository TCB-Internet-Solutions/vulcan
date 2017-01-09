using EPiServer.Core;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanClient : IElasticClient
    {
        ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null,
                bool includeNeutralLanguage = false,
                IEnumerable<ContentReference> rootReferences = null,
                IEnumerable<Type> typeFilter = null,
                IPrincipal principleReadFilter = null) where T : class, IContent;

        void IndexContent(IContent content);

        void DeleteContent(IContent content);

        void DeleteContent(ContentReference contentLink);

        string IndexName { get; }

        CultureInfo Language { get; }

        void AddSynonym(string term, string [] synonyms, bool biDirectional);

        void RemoveSynonym(string term);

        Dictionary<string, KeyValuePair<string[], bool>> GetSynonyms();
    }
}
