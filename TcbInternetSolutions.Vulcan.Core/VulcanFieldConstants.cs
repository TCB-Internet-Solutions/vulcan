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
        public const string AnalyzedModifier = "analyzed";

        /// <summary>
        /// Ancestors field
        /// </summary>
        public const string Ancestors = "__ancestors";

        /// <summary>
        /// Read permission field
        /// </summary>
        public const string ReadPermission = "__readPermission";

        /// <summary>
        /// Custom contents field
        /// </summary>
        public const string CustomContents = "__customContents";

        /// <summary>
        /// Binary media contents field, needs mapper-attachments
        /// </summary>
        public const string MediaContents = "__mediaContents";

        /// <summary>
        /// Media string contents, needs a custom IVulcanByteToStringConverter
        /// </summary>
        public const string MediaStringContents = "__mediaStringContents";

        /// <summary>
        /// Type field
        /// </summary>
        public const string TypeField = "_type";

        /// <summary>
        /// Used by default build search hit, but requires a custom Index Modifier to set.
        /// </summary>
        public const string SearchDescriptionField = "_vulcanSearchDescription";

        /// <summary>
        /// Filters for classes that are not abstracts
        /// </summary>
        public static Func<Type, bool> DefaultFilter = (x => x.IsClass && !x.IsAbstract);

        /// <summary>
        /// Filters for types not abstracts
        /// </summary>
        public static Func<Type, bool> AbstractFilter = (x => !x.IsAbstract);
    }
}
