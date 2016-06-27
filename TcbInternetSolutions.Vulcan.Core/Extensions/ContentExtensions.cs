using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    public static class ContentExtensions
    {
        private static ILogger Logger = LogManager.GetLogger();

        public static Injected<IVulcanHandler> VulcanHandler { get; set; }

        public static T GetContent<T>(this Nest.IHit<IContent> hit) where T : IContent =>
            ServiceLocator.Current.GetInstance<IContentLoader>().Get<T>(new ContentReference(hit.Id));

        public static T GetContent<T>(this IContent content) where T : IContent =>
            ServiceLocator.Current.GetInstance<IContentLoader>().Get<T>(content.ContentLink);

        public static IEnumerable<IContent> GetContents(this ISearchResponse<IContent> searchResponse) => GetContentsWorker<IContent>(searchResponse);

        public static IEnumerable<T> GetContents<T>(this ISearchResponse<IContent> searchResponse) where T : class, IContent =>
            GetContentsWorker<T>(searchResponse);

        public static string GetTypeName(this IContent content) =>
            content.GetType().Name.EndsWith("Proxy") ? content.GetType().BaseType.FullName : content.GetType().FullName;

        private static IEnumerable<T> GetContentsWorker<T>(ISearchResponse<IContent> searchResponse) where T : class, IContent
        {
            var list = new List<T>();

            if (searchResponse != null && searchResponse.Documents != null)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

                foreach (var document in searchResponse.Documents)
                {
                    if (document is IVulcanContentHit)
                    {
                        try
                        {
                            var content = contentLoader.Get<T>((document as IVulcanContentHit).ContentLink);

                            if (content != null)
                            {
                                list.Add(content);
                            }
                            else
                            {
                                Logger.Warning("Vulcan found a content in the index that was missing with content link: " + (document as IVulcanContentHit).ContentLink.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Warning("Vulcan found a content in the index that could not be loaded with content link: " + (document as IVulcanContentHit).ContentLink.ToString(), e);
                        }
                    }
                }
            }

            return list;
        }

        public static IDictionary<IHit<T>, T> GetHitContents<T>(this ISearchResponse<T> searchResponse) where T : class, IContent
        {
            var resolved = new Dictionary<string, Type>();

            var dic = new Dictionary<IHit<T>, T>();

            if(searchResponse != null && searchResponse.Hits != null)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

                foreach (var hit in searchResponse.Hits)
                {
                    try
                    {
                        Type contentType = null;

                        if(resolved.ContainsKey(hit.Type))
                        {
                            contentType = resolved[hit.Type];
                        }
                        else
                        {
                            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                var type = assembly.GetType(hit.Type, false);

                                if(type != null)
                                {
                                    contentType = type;
                                    resolved.Add(hit.Type, type);
                                }
                            }
                        }

                        if (contentType != null && typeof(IContent).IsAssignableFrom(contentType))
                        {
                            // we have content!

                            var content = contentLoader.Get<T>(new ContentReference(hit.Id));

                            if (content != null)
                            {
                                dic.Add(hit, content);
                            }
                            else
                            {
                                Logger.Warning("Vulcan found a content within hits that was missing with content link: " + hit.Id);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Logger.Information("Vulcan observed a non-content type: " + hit.Type, e);
                    }
                }
            }

            return dic;
        }
    }
}
