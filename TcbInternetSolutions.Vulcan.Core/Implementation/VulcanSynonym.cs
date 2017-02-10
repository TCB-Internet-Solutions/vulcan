namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Vulcan synonym
    /// </summary>
    public class VulcanSynonym
    {
        /// <summary>
        /// Language name
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Term
        /// </summary>
        public string Term { get; set; }

        /// <summary>
        /// Synonym list
        /// </summary>
        public string[] Synonyms { get; set; }

        /// <summary>
        /// Bi-directional
        /// </summary>
        public bool BiDirectional { get; set; }
    }
}
