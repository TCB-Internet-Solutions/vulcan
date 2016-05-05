using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanClient : ElasticClient, IVulcanClient
    {
        private static ILogger Logger = LogManager.GetLogger();

        public Injected<IContentLoader> ContentLoader { get; set; }
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        private CultureInfo cultureInfo { get; set; }
        private string index { get; set; }

        public VulcanClient(string index, ConnectionSettings settings, CultureInfo language)
            : base(settings)
        {
            if(language == null)
            {
                throw new Exception("Vulcan client requires a language (you may use CultureInfo.InvariantCulture if needed for non-language specific data)");
            }

            cultureInfo = language;
            this.index = index;
        }

        public ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null, bool includeNeutralLanguage = false) where T : class, EPiServer.Core.IContent
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

            var indexName = VulcanHelper.GetIndexName(index, cultureInfo);
            if(cultureInfo != CultureInfo.InvariantCulture && includeNeutralLanguage)
            {
                indexName += "," + VulcanHelper.GetIndexName(index, CultureInfo.InvariantCulture);
            }

            resolvedDescriptor = resolvedDescriptor.Index(indexName);

            Func<SearchDescriptor<T>, ISearchRequest> selector = ts => resolvedDescriptor;

            return base.Search<T, IContent>(selector);
        }

        public void IndexContent(IContent content)
        {
            if(content is ILocalizable && (content as ILocalizable).Language != cultureInfo)
            {
                throw new Exception("Cannot index content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name));
            }

            if(!(content is ILocalizable) && cultureInfo != CultureInfo.InvariantCulture)
            {
                throw new Exception("Cannot index content '" + GetId(content) + "' with no language with Vulcan client for language " + cultureInfo.Name);
            }

            if (!(content is IVersionable) || (content as IVersionable).Status == VersionStatus.Published)
            {
                try
                {
                    var response = base.Index(content, c => c.Id(GetId(content)).Type(GetTypeName(content)));

                    if (response.IsValid)
                    {
                        Logger.Debug("Vulcan indexed " + GetId(content) + " for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name) + ": " + response.DebugInformation);
                    }
                    else
                    {
                        throw new Exception(response.DebugInformation);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning("Vulcan could not index content with content link " + GetId(content) + " for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name) + ": ", e);
                }
            }
        }

        public void DeleteContent(IContent content)
        {
            if (content is ILocalizable && (content as ILocalizable).Language != cultureInfo)
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with language " + (content as ILocalizable).Language.Name + " with Vulcan client for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name));
            }

            if (!(content is ILocalizable) && cultureInfo != CultureInfo.InvariantCulture)
            {
                throw new Exception("Cannot delete content '" + GetId(content) + "' with no language with Vulcan client for language " + cultureInfo.Name);
            }

            try
            {
                var response = base.Delete(new DeleteRequest(VulcanHelper.GetIndexName(index, cultureInfo), GetTypeName(content), GetId(content)));

                Logger.Debug("Vulcan deleted " + GetId(content) + " for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name) + ": " + response.DebugInformation);
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not delete content with content link " + GetId(content) + " for language " + (cultureInfo == CultureInfo.InvariantCulture ? "invariant" : cultureInfo.Name) + ":", e);
            }
        }

        private string GetTypeName(IContent content)
        {
            return content.GetType().Name.EndsWith("Proxy") ? content.GetType().BaseType.FullName : content.GetType().FullName;
        }

        private string GetId(IContent content)
        {
            return content.ContentLink.ToReferenceWithoutVersion().ToString();
        }
    }
}
