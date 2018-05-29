using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using TcbInternetSolutions.Vulcan.Core.Implementation;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Content Extensions
    /// </summary>
    public static class ContentExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        /// <summary>
        /// Injected IContent Loader
        /// </summary>
        public static Injected<IContentLoader> ContentLoader { get; set; }

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
        public static T GetContent<T>(this IHit<IContent> hit) where T : IContent =>
            ContentLoader.Service.Get<T>(new ContentReference(hit.Id));

        /// <summary>
        /// Gets fully populated content from given content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T GetContent<T>(this IContent content) where T : IContent =>
            ContentLoader.Service.Get<T>(content.ContentLink);

        /// <summary>
        /// Converts search response to list of content
        /// </summary>
        /// <param name="searchResponse"></param>
        /// <param name="contentLoader"></param>
        /// <returns></returns>
        public static IEnumerable<IContent> GetContents(this ISearchResponse<IContent> searchResponse, IContentLoader contentLoader = null) => GetContentsWorker<IContent>(searchResponse, contentLoader);

        /// <summary>
        /// Converts search response to list of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchResponse"></param>
        /// <param name="contentLoader"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetContents<T>(this ISearchResponse<IContent> searchResponse, IContentLoader contentLoader = null) where T : class, IContent =>
            GetContentsWorker<T>(searchResponse, contentLoader);

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

            // ReSharper disable once InvertIf
            if (searchResponse?.Hits != null)
            {
                var contentLoader = ContentLoader.Service;

                foreach (var hit in searchResponse.Hits)
                {
                    try
                    {                        
                        if (!resolved.TryGetValue(hit.Type, out var contentType))
                        {
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                var type = assembly.GetType(hit.Type, false);

                                if (type == null) continue;

                                contentType = type;
                                resolved.Add(hit.Type, type);
                            }
                        }

                        if (contentType == null || !typeof(IContent).IsAssignableFrom(contentType)) continue;

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
        public static string GetTypeName(this IContent content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var contentType = content.GetType();
           
            return contentType.Name.EndsWith("Proxy") && contentType.BaseType != null ? contentType.BaseType.FullName : contentType.FullName;
        }

        private static IEnumerable<T> GetContentsWorker<T>(ISearchResponse<IContent> searchResponse, IContentLoader contentLoaderRef) where T : class, IContent
        {
            var list = new List<T>();
            if (searchResponse?.Documents == null) return list;

            var contentLoader = contentLoaderRef ?? VulcanHelper.GetService<IContentLoader>();

            foreach (var document in searchResponse.Documents)
            {
                if (!(document is IVulcanContentHit vulcanDocument)) continue;

                try
                {
                    var content = contentLoader.Get<T>(vulcanDocument.ContentLink);

                    if (content != null)
                    {
                        list.Add(content);
                    }
                    else
                    {
                        Logger.Warning("Vulcan found a content in the index that was missing with content link: " + vulcanDocument.ContentLink);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning("Vulcan found a content in the index that could not be loaded with content link: " + vulcanDocument.ContentLink, e);
                }
            }

            return list;
        }

        /// <summary>
        /// Gets file extension for mediadata
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public static string SearchFileExtension(this MediaData media)
        {
            if (media?.RouteSegment == null)
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
