using System;

namespace TcbInternetSolutions.Vulcan.Core
{
    /// <summary>
    /// Allows property to be ignored by vulcan indexer
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class VulcanIgnoreAttribute : Attribute { }
}