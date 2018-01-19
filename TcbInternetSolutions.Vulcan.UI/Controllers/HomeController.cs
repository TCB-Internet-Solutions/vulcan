using Elasticsearch.Net;
using EPiServer.Shell.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using TcbInternetSolutions.Vulcan.Core;
using TcbInternetSolutions.Vulcan.UI.Models.ViewModels;
using TcbInternetSolutions.Vulcan.UI.Support;

namespace TcbInternetSolutions.Vulcan.UI.Controllers
{
    /// <summary>
    /// UI Vulcan Controller
    /// </summary>
    [Authorize(Roles = "Administrators,CmsAdmins,WebAdmins,VulcanAdmins")]
    public class HomeController : Base.BaseController
    {
        private readonly IEnumerable<IVulcanIndexer> _VulcanIndexers;
        private readonly IEnumerable<IVulcanIndexingModifier> _VulcanIndexModifiers;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="vulcanHandler"></param>
        /// <param name="vulcanIndexers"></param>
        /// <param name="vulcanIndexModifiers"></param>
        public HomeController
        (
            IVulcanHandler vulcanHandler,
            IEnumerable<IVulcanIndexer> vulcanIndexers,
            IEnumerable<IVulcanIndexingModifier> vulcanIndexModifiers
        ) : base(vulcanHandler)
        {
            _VulcanIndexers = vulcanIndexers;
            _VulcanIndexModifiers = vulcanIndexModifiers;
        }

        /// <summary>
        ///  Main UI View
        /// </summary>
        /// <returns></returns>
        [MenuItem("/global/vulcan", Text = "Vulcan")]
        [HttpGet]
        public ActionResult Index()
        {
            var clients = VulcanHandler.GetClients()?.OrderBy(x => x.Language.EnglishName);

            var viewModel = new HomeViewModel()
            {
                VulcanClients = clients,
                VulcanHandler = VulcanHandler,
                PocoIndexers = _VulcanIndexers.OfType<IVulcanPocoIndexer>(),
                VulcanIndexModifiers = _VulcanIndexModifiers,
                ProtectedUiPath = EPiServer.Shell.Paths.ProtectedRootPath
            };

            if (clients?.Any() == true)
            {
                var healthResponse = clients.FirstOrDefault().CatIndices();

                viewModel.IndexHealthDescriptor.AddRange(healthResponse.Records.Where(r => r.Index.StartsWith(VulcanHandler.Index)));

                // doc types count
                Dictionary<string, Tuple<long, string, List<string>>> typeCounts = new Dictionary<string, Tuple<long, string, List<string>>>();

                foreach (var client in clients)
                {
                    string uiDisplayName = client.Language == CultureInfo.InvariantCulture ?
                        "non-specific" :
                        $"{client.Language.EnglishName} ({client.Language.Name})";

                    var typeCount = client.Search<object>(m => m.AllTypes()
                    .SearchType(SearchType.DfsQueryThenFetch). // possible 5x to 2x difference
                        Aggregations(aggs => aggs.Terms("typeCount", t => t.Field("_type")))).Aggregations["typeCount"] as Nest.BucketAggregate;

                    List<string> docCounts = new List<string>();
#if NEST2
                    var total = typeCount.Items.Sum(i => (i as Nest.KeyedBucket).DocCount) ?? 0;

                    foreach (Nest.KeyedBucket type in typeCount.Items)
                    {
                        docCounts.Add($"{type.Key}({type.DocCount})");
                    }
#elif NEST5
                    var total = typeCount.Items.Sum(i => (i as Nest.KeyedBucket<object>).DocCount) ?? 0;                   

                    foreach (Nest.KeyedBucket<object> type in typeCount.Items)
                    {
                        docCounts.Add($"{type.Key}({type.DocCount})");
                    }
#endif

                    typeCounts[client.IndexName] = Tuple.Create(total, uiDisplayName, docCounts);
                }

                viewModel.ClientViewInfo = typeCounts;
            }

            return View(Helper.ResolveView("Home/Index.cshtml"), viewModel);
        }
    }
}