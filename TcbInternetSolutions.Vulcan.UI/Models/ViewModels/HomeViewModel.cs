using System.Collections;
using System.Collections.Generic;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Models.ViewModels
{
    public class HomeViewModel
    {
        public IVulcanHandler VulcanHandler { get; set; }

        public IEnumerable<IVulcanPocoIndexer> PocoIndexers { get; set; }
    }
}
