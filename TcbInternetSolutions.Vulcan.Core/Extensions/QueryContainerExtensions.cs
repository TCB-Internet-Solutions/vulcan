namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    using EPiServer.Core;
    using EPiServer.Search;
    using Nest;
    using System;

    public static class QueryContainerExtensions
    {
        public static QueryContainer FilterForPublished<T>(this QueryContainer query, bool requireIsSearchable = false) where T : class, IContent
        {
            var notDeleted = new QueryContainerDescriptor<T>().Term(t => t.Field(xf => xf.IsDeleted).Value(false));
            var published = new QueryContainerDescriptor<IVersionable>().DateRange(dr => dr.LessThanOrEquals(DateTime.Now).Field(xf => xf.StartPublish));
            var notExpired = new QueryContainerDescriptor<IVersionable>().DateRange(dr => dr.GreaterThanOrEquals(DateTime.Now).Field(xf => xf.StopPublish));
            var notExpiredMissing = new QueryContainerDescriptor<IVersionable>().Missing(dr => dr.Field(xf => xf.StopPublish).NullValue().Existence());
            var searchable = new QueryContainerDescriptor<ISearchable>().Term(t => t.Field(xf => xf.IsSearchable).Value(true));

            if (!requireIsSearchable)
            {
                return new QueryContainerDescriptor<T>().Bool(b => b.Must(query).Filter(notDeleted && published && (notExpired || notExpiredMissing)));
            }

            return new QueryContainerDescriptor<T>().Bool(b => b.Must(query).Filter(searchable && notDeleted && published && (notExpired || notExpiredMissing)));
        }
    }
}
