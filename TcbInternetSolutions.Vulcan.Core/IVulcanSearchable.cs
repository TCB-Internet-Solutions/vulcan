namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Used to determine if content is searchable on front-end searches
    /// </summary>
    public interface IVulcanSearchable 
    {
        /// <summary>
        /// Used to determine if content is searchable on front-end searches
        /// </summary>
        bool IsSearchable { get; }
    }
}
