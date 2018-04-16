using System;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Vulcan Constants
    /// </summary>
    public class VulcanFieldConstants
    {
        /// <summary>
        /// Analyzed modifier
        /// </summary>
        public static readonly string AnalyzedModifier = "analyzed";

        /// <summary>
        /// Ancestors field
        /// </summary>
        public static readonly string Ancestors = "__ancestors";

        /// <summary>
        /// Read permission field
        /// </summary>
        public static readonly string ReadPermission = "__readPermission";

        /// <summary>
        /// Custom contents field
        /// </summary>
        public static readonly string CustomContents = "__customContents";

        /// <summary>
        /// Binary media contents field, needs mapper-attachments
        /// </summary>
        public static readonly string MediaContents = "__mediaContents";

        /// <summary>
        /// Media string contents, needs a custom IVulcanByteToStringConverter
        /// </summary>
        public static readonly string MediaStringContents = "__mediaStringContents";

        /// <summary>
        /// Type field
        /// </summary>
        public static readonly string TypeField = "_type";

        /// <summary>
        /// Used by default build search hit, but requires a custom Index Modifier to set.
        /// </summary>
        public static readonly string SearchDescriptionField = "_vulcanSearchDescription";

        /// <summary>
        /// Filters for classes that are not abstracts
        /// </summary>
        public static Func<Type, bool> DefaultFilter = x => x.IsClass && !x.IsAbstract;

        /// <summary>
        /// Filters for types not abstracts
        /// </summary>
        public static Func<Type, bool> AbstractFilter = x => !x.IsAbstract;
    }
}
