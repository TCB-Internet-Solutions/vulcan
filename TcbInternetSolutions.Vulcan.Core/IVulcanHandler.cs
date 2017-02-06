using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Used to help modifiers handle deletes.
    /// </summary>
    public delegate void IndexDeleteHandler(IEnumerable<string> deletedIndexes);

    public interface IVulcanHandler
    {
        string Index { get; }

        IVulcanClient GetClient(CultureInfo language = null);

        IVulcanClient[] GetClients();

        void DeleteIndex();

        IndexDeleteHandler DeletedIndices { get; set; }

        void DeleteContentByLanguage(IContent content);

        void DeleteContentEveryLanguage(ContentReference contentLink);

        void IndexContentByLanguage(IContent content);

        void IndexContentEveryLanguage(ContentReference contentLink);

        void IndexContentEveryLanguage(IContent content);

        IEnumerable<IVulcanIndexingModifier> IndexingModifers { get; }

        void AddConditionalContentIndexInstruction<T>(Func<T, bool> instruction) where T : IContent;

        bool AllowContentIndexing(IContent objectToIndex);
    }
}
