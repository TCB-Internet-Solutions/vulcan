using System;

namespace TcbInternetSolutions.Vulcan.Core
{
    public class VulcanFieldConstants
    {
        public const string Ancestors = "__ancestors";

        public const string ReadPermission = "__readPermission";

        public const string CustomContents = "__customContents";

        public const string MediaContents = "__mediaContents";

        public static Func<Type, bool> DefaultFilter = (x => x.IsClass && !x.IsAbstract);

        public static Func<Type, bool> AbstractFilter = (x => !x.IsAbstract);
    }
}
