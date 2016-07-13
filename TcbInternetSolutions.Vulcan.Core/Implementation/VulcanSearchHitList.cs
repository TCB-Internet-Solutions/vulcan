namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using Nest;
    using System.Collections.Generic;
    using System.Linq;

    public class VulcanSearchHitList
    {
        private long _Took;

        public VulcanSearchHitList() : this(new List<VulcanSearchHit>()) { }

        public VulcanSearchHitList(IEnumerable<VulcanSearchHit> results)
        {
            Items = results?.ToList();
            _Took = -1;
        }

        public virtual ISearchResponse<IContent> ResponseContext { get; set; }

        public virtual List<VulcanSearchHit> Items { get; set; }

        public virtual int Page { get; set; }

        public virtual int PageSize { get; set; }

        public virtual long TotalHits { get; set; }

        public virtual long Took
        {
            get
            {
                if (_Took < -1 && ResponseContext != null)
                    _Took = ResponseContext.Took;

                return _Took;
            }
            set
            {
                _Took = value;
            }

        }
    }
}
