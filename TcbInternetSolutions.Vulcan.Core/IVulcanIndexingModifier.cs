using System.IO;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Index modifier
    /// </summary>
    public interface IVulcanIndexingModifier
    {
        /// <summary>
        /// Process modifier and flush customization to stream
        /// </summary>
        /// <param name="modifierArgs"></param>
        void ProcessContent(IVulcanIndexingModifierArgs modifierArgs);//, Stream writableStream);
    }
}
