using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.UI.Models.ViewModels
{
    public class HomeViewModel
    {
        public Dictionary<string, Tuple<long, string, List<string>>> ClientViewInfo { get; set; }

        public List<CatIndicesRecord> IndexHealthDescriptor { get; set; } = new List<CatIndicesRecord>();

        public IEnumerable<IVulcanPocoIndexer> PocoIndexers { get; set; }

        public IEnumerable<IVulcanClient> VulcanClients { get; set; }

        public IVulcanHandler VulcanHandler { get; set; }

        public IEnumerable<IVulcanIndexingModifier> VulcanIndexModifiers { get; set; }

        public bool HasClients => VulcanClients?.Any() == true;

        public bool HasIndexModifiers => VulcanIndexModifiers?.Any() == true;

        public bool HasPocoIndexers => PocoIndexers?.Any() == true;

        public string ProtectedUiPath { get; set; }
    }
}
