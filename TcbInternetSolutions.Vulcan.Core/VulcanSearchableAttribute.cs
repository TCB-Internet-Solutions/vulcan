namespace TcbInternetSolutions.Vulcan.Core
{
    using System;

    /// <summary>
    /// Allows properties to be indexed in custom search field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class VulcanSearchableAttribute : Attribute { }
}
