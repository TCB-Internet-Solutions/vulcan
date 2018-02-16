namespace TcbInternetSolutions.Vulcan.Core
{
    using System;

    /// <summary>
    /// Allows properties to be indexed in custom search field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VulcanSearchableAttribute : Attribute { }
}
