using System.Collections;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Models.ViewModels
{
    // todo: add many things here so that compiler flags can be used to toggle for nest versions

    public class HomeViewModel
    {
        public IVulcanHandler VulcanHandler { get; set; }

        public IEnumerable<IVulcanPocoIndexer> PocoIndexers { get; set; }
    }
}
