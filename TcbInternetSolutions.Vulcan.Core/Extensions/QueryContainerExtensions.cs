namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    using EPiServer.Core;
    using Nest;
    using System;

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
            //var expiredMissing = new QueryContainerDescriptor<IVersionable>().Missing(dr => dr.Field(xf => xf.StopPublish).NullValue().Existence());
            var expiredExists = new QueryContainerDescriptor<IVersionable>().Exists(dr => dr.Field(xf => xf.StopPublish));

            return !requireIsSearchable ?
                new QueryContainerDescriptor<T>().Bool(b => b.Must(query).MustNot(notExpired || expiredExists).Filter(notDeleted && published)) :
                new QueryContainerDescriptor<T>().Bool(b => b.Must(query).MustNot(notExpired || expiredExists).Filter(searchable && notDeleted && published));
        }
    }
}
