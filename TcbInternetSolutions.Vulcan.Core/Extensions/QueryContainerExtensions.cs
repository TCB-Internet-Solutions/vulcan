namespace TcbInternetSolutions.Vulcan.Core.Extensions
{
    using EPiServer.Core;
    using Nest;
    using System;

    public static class QueryContainerExtensions
    {
        public static QueryContainer FilterForPublished<T>(this QueryContainer query) where T : class, IContent
        {
            var notDeleted = new QueryContainerDescriptor<T>().Term(t => t.Field(xf => xf.IsDeleted).Value(false));
            var published = new QueryContainerDescriptor<T>().DateRange(dr => dr.LessThanOrEquals(DateTime.Now).Field(xf => (xf as IVersionable).StartPublish));
            var notExpired = new QueryContainerDescriptor<T>().DateRange(dr => dr.GreaterThanOrEquals(DateTime.Now).Field(xf => (xf as IVersionable).StopPublish));

            return new QueryContainerDescriptor<T>().Bool(b => b.Must(query && notDeleted && published && notExpired));
        }
    }
}
