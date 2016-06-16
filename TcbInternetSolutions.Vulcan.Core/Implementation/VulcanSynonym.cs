using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcbInternetSolutions.Vulcan.Core.Implementation
{
    public class VulcanSynonym
    {
        public string Language { get; set; }

        public string Term { get; set; }

        public string[] Synonyms { get; set; }

        public bool BiDirectional { get; set; }
    }
}
