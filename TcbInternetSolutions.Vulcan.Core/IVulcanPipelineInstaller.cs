using EPiServer.Framework.Initialization;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Installs a pipeline on Epi initialization
    /// </summary>
    public interface IVulcanPipelineInstaller
    {
        /// <summary>
        /// Name/Id of pipeline, must match an IVulcanPipeline
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Exectutes put pipeline and any other tasks needed, may throw exceptions if setup doesn't meet requirements
        /// </summary>
        /// <param name="vulcanClient"></param>        
        /// <returns></returns>
        void Install(IVulcanClient vulcanClient);
    }
}
