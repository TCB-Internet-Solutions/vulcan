using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Search;
using Mediachase.Search.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce.SearchProviders
{
    public class VulcanSearchProvider : SearchProvider
    {
        public Injected<IVulcanHandler> VulcanHandler { get; set; }

        public Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public Injected<IContentLoader> ContentLoader { get; set; }

        public override string QueryBuilderType => null;// "TcbInternetSolutions.Vulcan.Commerce.SearchProviders.VulcanSearchQueryBuilder, TcbInternetSolutions.Vulcan.Commerce.SearchProviders";

        public override void Close(string applicationName, string scope)
        {
        }

        public override void Commit(string applicationName)
        {
        }

        public override void Index(string applicationName, string scope, ISearchDocument document)
        {
        }

        public override int Remove(string applicationName, string scope, string key, string value) => 0;

        public override void RemoveAll(string applicationName, string scope)
        {
        }

        public override ISearchResults Search(string applicationName, ISearchCriteria criteria)
        {
            if (criteria is CatalogEntrySearchCriteria)
            {
                var cesc = criteria as CatalogEntrySearchCriteria;

                if (cesc.ClassTypes != null && cesc.ClassTypes.Count == 1)
                {
                    // in this provider, we only support a single class type

                    switch(cesc.ClassTypes[0].ToUpper())
                    {
                        case "VARIANT":
                            return Search<VariationContent>(criteria);

                        case "PRODUCT":
                            return Search<ProductContent>(criteria);

                        case "BUNDLE":
                            return Search<BundleContent>(criteria);

                        case "PACKAGE":
                        case "DYNAMICPACKAGE": // for Vulcan purposes, dynamic packages are the same as packages
                            return Search<PackageContent>(criteria);
                    }
                }
            }

            return Search<EntryContentBase>(criteria);
        }

        private ISearchResults Search<T>(ISearchCriteria criteria) where T : EntryContentBase
        {
            // set up filters

            // 1: phrase

            var filters = new List<Nest.QueryContainer>();

            if(criteria is CatalogEntrySearchCriteria)
            {
                var cesc = criteria as CatalogEntrySearchCriteria;

                if (!string.IsNullOrWhiteSpace(cesc.SearchPhrase))
                {
                    filters.Add(new Nest.QueryContainerDescriptor<T>().SimpleQueryString(
                        sq => sq.Fields(f => f.Field("*.analyzed")).Query(cesc.SearchPhrase.Trim())));
                }
            }

            // 2: id ... NOTE: Vulcan supports 1 and only 1 filter field, code

            if(criteria.ActiveFilterFields != null && criteria.ActiveFilterFields.Count() == 1 && criteria.ActiveFilterFields[0].Equals("code", StringComparison.InvariantCultureIgnoreCase))
            {
                filters.Add(new Nest.QueryContainerDescriptor<T>().Term(
                            p => p.Field(f => f.Code).Value((criteria.ActiveFilterValues[0] as SimpleValue).value)));
            }

            // 3: inactive... TODO, not sure what this should check!
            /*
            if(!criteria.IncludeInactive)
            {
                filters.Add(new Nest.QueryContainerDescriptor<T>().Term(
                    p => p.Field(f => f.)
            }*/

            // get catalog filter, if needed

           var catalogReferences = new List<ContentReference>();

            if (criteria is CatalogEntrySearchCriteria)
            {
                var cesc = criteria as CatalogEntrySearchCriteria;

                if(cesc.CatalogNames != null)
                {
                    var catalogs = ContentLoader.Service.GetChildren<CatalogContent>(ReferenceConverter.Service.GetRootLink());

                    if (catalogs != null && catalogs.Any())
                    {
                        foreach (var catalogName in cesc.CatalogNames)
                        {
                            var catalog = catalogs.FirstOrDefault(c => c.Name.Equals(catalogName, StringComparison.InvariantCultureIgnoreCase));

                            if(catalog != null)
                            {
                                catalogReferences.Add(catalog.ContentLink);
                            }
                        }
                    }
                }
            }

            if (!catalogReferences.Any()) catalogReferences = null;

            // do search

            var searchDescriptor = new Nest.SearchDescriptor<T>();

            searchDescriptor.Skip(criteria.StartingRecord);
            searchDescriptor.Take(criteria.RecordsToRetrieve);
            
            if(filters.Any())
            {
                searchDescriptor.Query(q => q.Bool(b => b.Must(filters.ToArray())));
            }

            var client = VulcanHandler.Service.GetClient(new CultureInfo(criteria.Locale));

            var results = client.SearchContent<T>(q => searchDescriptor, false, catalogReferences);

            //var id = ReferenceConverter.Service.GetObjectId();

            var searchDocuments = new SearchDocuments() { TotalCount = Convert.ToInt32(results.Total) };

            if(results.Hits != null && results.Hits.Any())
            {
                foreach(var hit in results.Hits)
                {
                    var doc = new SearchDocument();
                    doc.Add(new SearchField("_id", ReferenceConverter.Service.GetObjectId(hit.Source.ContentLink)));

                    searchDocuments.Add(doc);
                }
            }

            return new SearchResults(searchDocuments, criteria);
        }
    }
}
