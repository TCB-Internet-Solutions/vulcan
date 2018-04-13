using EPiServer.Core;
using System;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    /// <summary>
    /// Determines if content can be indexed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VulcanConditionalContentIndexInstruction<T> : IVulcanConditionalContentIndexInstruction where T : IContent
    {
        /// <summary>
        /// Func to determine indexing
        /// </summary>
        public Func<T, bool> Condition { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="condition"></param>
        public VulcanConditionalContentIndexInstruction(Func<T, bool> condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// Determines if content can be indexed
        /// </summary>
        /// <param name="objectToIndex"></param>
        /// <returns></returns>
        public bool AllowContentIndexing(IContent objectToIndex)
        {
            return Condition?.Invoke((T)objectToIndex) ?? true;
        }
    }
}
