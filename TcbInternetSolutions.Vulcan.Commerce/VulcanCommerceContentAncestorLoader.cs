using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using TcbInternetSolutions.Vulcan.Core;

namespace TcbInternetSolutions.Vulcan.Commerce
{
    // todo: review how to fix warnings in VulcanCommerceContentAncestorLoader

    /// <summary>
    /// Gets ancestors for CMS content
    /// </summary>
    [ServiceConfiguration(typeof(IVulcanContentAncestorLoader), Lifecycle = ServiceInstanceScope.Singleton)]
    public class VulcanCommerceContentAncestorLoader : IVulcanContentAncestorLoader
    {
        private readonly IContentLoader _contentLoader;
        private readonly IRelationRepository _relationRepository;

        /// <summary>
        /// DI Constructor
        /// </summary>
        /// <param name="contentLoader"></param>        
        /// <param name="relationRepository"></param>
        public VulcanCommerceContentAncestorLoader(IContentLoader contentLoader,IRelationRepository relationRepository)
        {
            _contentLoader = contentLoader;
            _relationRepository = relationRepository;
        }

        public IEnumerable<ContentReference> GetAncestors(IContent content)
        {
            var ancestors = new List<ContentReference>();

            if (content is VariationContent)
            {
                //var productAncestors = _RelationRepository.GetParents<Relation>(content.ContentLink);
                var productAncestors = _relationRepository.GetRelationsByTarget(content.ContentLink)?.OfType<ProductVariation>().ToList();

                if (productAncestors?.Any() == true)
                {
                    ancestors.AddRange(productAncestors.Select(GetLinkFromProductVariant));
                    ancestors.AddRange(productAncestors.SelectMany(pa => GetAncestorCategoriesIterative(GetLinkFromProductVariant(pa), false)));
                }
            }

            // for these purposes, we assume that products cannot exist inside other products
            // variant may also exist directly inside a category
            ancestors.AddRange(GetAncestorCategoriesIterative(content.ContentLink, false));

            return ancestors.Distinct();
        }

        private IEnumerable<ContentReference> GetAncestorCategoriesIterative(ContentReference contentLink, bool checkCategoryParent)
        {
            var ancestors = new List<ContentReference>();
            IEnumerable<Relation> categories;

            try
            {
                categories = _relationRepository.GetRelationsBySource<NodeRelation>(contentLink)?.ToList();
            }
            catch (Exception)
            {
                // probably not a valid category or node type to pull the relations of, so stop the iteration here
                return ancestors;
            }

            if (categories?.Any() == true)
            {
                ancestors.AddRange(categories.Select(GetLinkFromRelation));
                ancestors.AddRange(categories.SelectMany(c => GetAncestorCategoriesIterative(GetLinkFromRelation(c), true)));
            }

            // ReSharper disable once InvertIf
            if (checkCategoryParent && _contentLoader.Get<IContent>(contentLink) is NodeContent thisCat && !ancestors.Contains(thisCat.ParentLink))
            {
                ancestors.Add(thisCat.ParentLink);

                ancestors.AddRange(GetAncestorCategoriesIterative(thisCat.ParentLink, true));
            }

            return ancestors;
        }

        private static ContentReference GetLinkFromProductVariant(ProductVariation p)
        {
            return p.Source; // should this be Parent?
        }

        private static ContentReference GetLinkFromRelation(Relation n)
        {
            return n.Target; // should this be Child?
        }
    }
}