using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanConditionalContentIndexInstruction<T> : IVulcanConditionalContentIndexInstruction where T : IContent
    {
        public Func<T, bool> Condition { get; private set; }

        public VulcanConditionalContentIndexInstruction(Func<T, bool> condition)
        {
            Condition = condition;
        }

        public bool AllowContentIndexing(IContent objectToIndex)
        {
            return Condition.Invoke((T)objectToIndex);
        }
    }
}
