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

    /// <summary>
    /// Vulcan handler contract
    /// </summary>
    public interface IVulcanHandler
    {
        /// <summary>
        /// Delete indices handler
        /// </summary>
        IndexDeleteHandler DeletedIndices { get; set; }

        /// <summary>
        /// Index name
        /// </summary>
        string Index { get; }

        /// <summary>
        /// Index modifier list
        /// </summary>
        IEnumerable<IVulcanIndexingModifier> IndexingModifers { get; }

        /// <summary>
        /// Adds instruction for indexing rules
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instruction"></param>
        void AddConditionalContentIndexInstruction<T>(Func<T, bool> instruction) where T : IContent;

        /// <summary>
        /// Determines if content can be indexed
        /// </summary>
        /// <param name="objectToIndex"></param>
        /// <returns></returns>
        bool AllowContentIndexing(IContent objectToIndex);

        /// <summary>
        /// Delete content by language
        /// </summary>
        /// <param name="content"></param>
        void DeleteContentByLanguage(IContent content);

        /// <summary>
        /// Delete content for all languages
        /// </summary>
        /// <param name="contentLink"></param>
        /// <param name="typeName"></param>
        void DeleteContentEveryLanguage(ContentReference contentLink, string typeName);

        /// <summary>
        /// Delete index
        /// </summary>
        void DeleteIndex();

        /// <summary>
        /// Get vulcan client by culture
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        IVulcanClient GetClient(CultureInfo language = null);

        /// <summary>
        /// Get all vulcan clients
        /// </summary>
        /// <returns></returns>
        IVulcanClient[] GetClients();

        /// <summary>
        /// Index content by language
        /// </summary>
        /// <param name="content"></param>
        void IndexContentByLanguage(IContent content);

        /// <summary>
        /// Index content for all languages
        /// </summary>
        /// <param name="contentLink"></param>
        void IndexContentEveryLanguage(ContentReference contentLink);

        /// <summary>
        /// Index content for all languages
        /// </summary>
        /// <param name="content"></param>
        void IndexContentEveryLanguage(IContent content);
    }
}
