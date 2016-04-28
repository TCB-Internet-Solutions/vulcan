using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
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

        public VulcanClient(ConnectionSettings settings) : base(settings)
        {
        }

        public ISearchResponse<IContent> SearchContent<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchDescriptor = null, string language = null) where T : class, EPiServer.Core.IContent
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
                types.AddRange(assembly.GetTypes().Where(t => typeof(T).IsAssignableFrom(t)).Select(t => t.FullName));
            }

            resolvedDescriptor = resolvedDescriptor.Type(string.Join(",", types)).ConcreteTypeSelector((d, docType) => typeof(VulcanContentHit));

            Func<QueryContainerDescriptor<T>, QueryContainer> queryFunc = q => q.Bool(b => b.Must(f => f.Term("language", "en")));

            var queryContainer = queryFunc.Invoke(new QueryContainerDescriptor<T>());

            var existingQueryContainer = resolvedDescriptor.GetType().GetProperty("Nest.ISearchRequest.Query", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(resolvedDescriptor, null) as QueryContainer;

            if(existingQueryContainer != null)
            {
                queryContainer = queryContainer & existingQueryContainer;
            }

            resolvedDescriptor.GetType().InvokeMember("Nest.ISearchRequest.Query", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty, Type.DefaultBinder, resolvedDescriptor, new object[] { queryContainer });

            Func<SearchDescriptor<T>, ISearchRequest> selector = ts => resolvedDescriptor;

            return base.Search<T, IContent>(selector);
        }

        public void IndexContent(IContent content)
        {
            if (!(content is IVersionable) || (content as IVersionable).Status == VersionStatus.Published)
            {
                try
                {
                    if (content is ILocalizable)
                    {
                        foreach (var language in (content as ILocalizable).ExistingLanguages)
                        {
                            var id = GetId(content.ContentLink, language.Name);

                            var localizedContent = (content as ILocalizable).Language == language ? content : ContentLoader.Service.Get<IContent>(content.ContentLink.ToReferenceWithoutVersion(), language);

                            if ((localizedContent as IVersionable).Status == VersionStatus.Published) // need to recheck for other languages
                            {
                                var response = base.Index(localizedContent, c => c.Id(id).Type(GetTypeName(content)));

                                Logger.Debug("Vulcan indexed " + id + ": " + response.DebugInformation);
                            }
                        }
                    }
                    else
                    {
                        var response = base.Index(content, c => c.Id(GetId(content.ContentLink, null)).Type(GetTypeName(content)));

                        Logger.Debug("Vulcan indexed " + GetId(content.ContentLink, null) + ": " + response.DebugInformation);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning("Vulcan could not index content with content link: " + content.ContentLink.ToString(), e);
                }
            }
        }

        public void DeleteContent(IContent content)
        {
            try
            {
                if (content is ILocalizable)
                {
                    foreach (var language in (content as ILocalizable).ExistingLanguages)
                    {
                        var id = GetId(content.ContentLink, language.Name);

                        var response = base.Delete(new DeleteRequest(VulcanHelper.Index, GetTypeName(content), id));

                        Logger.Debug("Vulcan deleted " + id + ": " + response.DebugInformation);
                    }
                }
                else
                {
                    var response = base.Delete(new DeleteRequest(VulcanHelper.Index, GetTypeName(content), GetId(content.ContentLink, null)));

                    Logger.Debug("Vulcan deleted " + GetId(content.ContentLink, null) + ": " + response.DebugInformation);
                }
            }
            catch (Exception e)
            {
                Logger.Warning("Vulcan could not delete content with content link: " + content.ContentLink.ToString(), e);
            }
        }

        private string GetTypeName(IContent content)
        {
            return content.GetType().Name.EndsWith("Proxy") ? content.GetType().BaseType.FullName : content.GetType().FullName;
        }

        private string GetId(ContentReference contentLink, string language)
        {
            return contentLink.ToReferenceWithoutVersion().ToString() + (language == null ? "" : "~" + language);
        }
    }
}
