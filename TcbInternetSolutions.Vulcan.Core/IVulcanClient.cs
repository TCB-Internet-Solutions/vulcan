using EPiServer.Core;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core
{
    public interface IVulcanClient : IElasticClient
    {
        ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null, string language = null) where T : class, IContent;

        void IndexContent(IContent content);

        void DeleteContent(IContent content);
    }
}
