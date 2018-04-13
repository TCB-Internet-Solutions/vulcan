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
        private long _took;

        private List<VulcanSearchHit> _results;

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
            _took = -1;
            _results = results?.ToList();
        }
        
        /// <summary>
        /// Vulcan response context
        /// </summary>
        public virtual ISearchResponse<IContent> ResponseContext { get; set; }

        /// <summary>
        /// Found items
        /// </summary>
        public virtual List<VulcanSearchHit> Items
        {
            get => _results;
            set => _results = value;
        }

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
                if (_took < -1 && ResponseContext != null)
                {
#if NEST2
                    _took = ResponseContext.TookAsLong;
#elif NEST5
                    _took = ResponseContext.Took;
#endif
                }

                return _took;
            }
            set
            {
                _took = value;
            }

        }
    }
}
