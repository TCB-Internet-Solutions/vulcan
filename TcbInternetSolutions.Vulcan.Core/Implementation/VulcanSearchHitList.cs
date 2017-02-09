namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    using EPiServer.Core;
    using Nest;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Vulcan search hit list
    /// </summary>
    public class VulcanSearchHitList
    {
        private long _Took;

        /// <summary>
        /// Constructor
        /// </summary>
        public VulcanSearchHitList() : this(new List<VulcanSearchHit>()) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="results"></param>
        public VulcanSearchHitList(IEnumerable<VulcanSearchHit> results)
        {
            Items = results?.ToList();
            _Took = -1;
        }

        /// <summary>
        /// Vulcan response context
        /// </summary>
        public virtual ISearchResponse<IContent> ResponseContext { get; set; }

        /// <summary>
        /// Found items
        /// </summary>
        public virtual List<VulcanSearchHit> Items { get; set; }

        /// <summary>
        /// Page
        /// </summary>
        public virtual int Page { get; set; }

        /// <summary>
        /// Pagesize
        /// </summary>
        public virtual int PageSize { get; set; }

        /// <summary>
        /// Total items 
        /// </summary>
        public virtual long TotalHits { get; set; }

        /// <summary>
        /// Search time
        /// </summary>
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
