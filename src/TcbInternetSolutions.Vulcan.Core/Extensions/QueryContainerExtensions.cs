using EPiServer.Core;
using Nest;
using System;

namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    /// <summary>
    /// Query extensions
    /// </summary>
    public static class QueryContainerExtensions
    {
        /// <summary>
        /// Adds published filter for expired, deleted
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>        
        /// <param name="requireIsSearchable"></param>        
        /// <returns></returns>
        public static QueryContainer FilterForPublished<T>(this QueryContainer query, bool requireIsSearchable = false) where T : class, IContent
        {
            var notDeleted = new QueryContainerDescriptor<T>().Term(t => t.Field(xf => xf.IsDeleted).Value(false));
            var published = new QueryContainerDescriptor<IVersionable>().DateRange(dr => dr.LessThanOrEquals(DateTime.Now).Field(xf => xf.StartPublish));
            var notExpired = new QueryContainerDescriptor<IVersionable>().DateRange(dr => dr.GreaterThanOrEquals(DateTime.Now).Field(xf => xf.StopPublish));
            var searchable = new QueryContainerDescriptor<IVulcanSearchable>().Term(t => t.Field(xf => xf.IsSearchable).Value(true));
            var expiredExists = new QueryContainerDescriptor<IVersionable>().Exists(dr => dr.Field(xf => xf.StopPublish));
            var filtered = notDeleted && published && (notExpired && expiredExists || !expiredExists);

            if (requireIsSearchable)
                filtered = filtered && searchable;

            return new QueryContainerDescriptor<T>().Bool(b => b.Must(query).Filter(filtered));
        }
    }
}
