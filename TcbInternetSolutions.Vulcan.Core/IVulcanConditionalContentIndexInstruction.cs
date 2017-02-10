using EPiServer.Core;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Determines if content can be indexed
    /// </summary>
    public interface IVulcanConditionalContentIndexInstruction
    {
        /// <summary>
        /// Determines if content can be indexed
        /// </summary>
        /// <param name="objectToIndex"></param>
        /// <returns></returns>
        bool AllowContentIndexing(IContent objectToIndex);
    }
}
