namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Vulcan search hit
    /// </summary>
    public class VulcanSearchHit
    {
        /// <summary>
        /// Id
        /// </summary>
        public virtual object Id { get; set; }

        /// <summary>
        /// Summary
        /// </summary>
        public virtual string Summary { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public virtual string Title { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        public virtual string Url { get; set; }
    }
}