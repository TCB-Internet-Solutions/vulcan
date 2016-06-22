using EPiServer.Core;
using System.Collections.Generic;
using System.Globalization;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanHandler
    {
        string Index { get; }

        IVulcanClient GetClient(CultureInfo language = null);

        IVulcanClient[] GetClients();

        void DeleteIndex();

        void DeleteContentByLanguage(IContent content);

        void DeleteContentEveryLanguage(IContent content);

        void IndexContentByLanguage(IContent content);

        void IndexContentEveryLanguage(IContent content);

        IEnumerable<IVulcanIndexingModifier> IndexingModifers { get; }
    }
}
