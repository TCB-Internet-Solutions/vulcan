using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Content Extensions
    /// </summary>
    public static class ContentExtensions
    {
        private static ILogger Logger = LogManager.GetLogger();

        /// <summary>
        /// Injected VulcanHanlder
        /// </summary>
        public static Injected<IVulcanHandler> VulcanHandler { get; set; }

        /// <summary>
        /// Converts hit to IContent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static T GetContent<T>(this Nest.IHit<IContent> hit) where T : IContent =>
            ServiceLocator.Current.GetInstance<IContentLoader>().Get<T>(new ContentReference(hit.Id));

        /// <summary>
        /// Gets fully populated content from given content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T GetContent<T>(this IContent content) where T : IContent =>
            ServiceLocator.Current.GetInstance<IContentLoader>().Get<T>(content.ContentLink);

        /// <summary>
        /// Converts search response to list of content
        /// </summary>
        /// <param name="searchResponse"></param>
        /// <returns></returns>
        public static IEnumerable<IContent> GetContents(this ISearchResponse<IContent> searchResponse) => GetContentsWorker<IContent>(searchResponse);

        /// <summary>
        /// Converts search response to list of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchResponse"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetContents<T>(this ISearchResponse<IContent> searchResponse) where T : class, IContent =>
            GetContentsWorker<T>(searchResponse);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchResponse"></param>
        /// <returns></returns>
        public static IDictionary<IHit<T>, T> GetHitContents<T>(this ISearchResponse<T> searchResponse) where T : class, IContent
        {
            var resolved = new Dictionary<string, Type>();

            var dic = new Dictionary<IHit<T>, T>();

            if (searchResponse != null && searchResponse.Hits != null)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

                foreach (var hit in searchResponse.Hits)
                {
                    try
                    {
                        Type contentType = null;

                        if (!resolved.TryGetValue(hit.Type, out contentType))
                        {
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                var type = assembly.GetType(hit.Type, false);

                                if (type != null)
                                {
                                    contentType = type;
                                    resolved.Add(hit.Type, type);
                                }
                            }
                        }

                        if (contentType != null && typeof(IContent).IsAssignableFrom(contentType))
                        {
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
                    catch (Exception e)
                    {
                        Logger.Information("Vulcan observed a non-content type: " + hit.Type, e);
                    }
                }
            }

            return dic;
        }

        /// <summary>
        /// Gets typename for content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
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

        public static string SearchFileExtension(this MediaData media)
        {
            if (media == null)
                return string.Empty;

            try
            {
                return Path.GetExtension(media.RouteSegment).Replace(".", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
