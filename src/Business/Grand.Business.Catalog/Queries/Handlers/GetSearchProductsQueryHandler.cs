using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Queries.Handlers
{
    public class GetSearchProductsQueryHandler : IRequestHandler<GetSearchProductsQuery, (IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)>
    {

        private readonly IRepository<Product> _productRepository;
        private readonly ISpecificationAttributeService _specificationAttributeService;

        private readonly CatalogSettings _catalogSettings;

        public GetSearchProductsQueryHandler(
            IRepository<Product> productRepository,
            ISpecificationAttributeService specificationAttributeService,
            CatalogSettings catalogSettings)
        {
            _productRepository = productRepository;
            _specificationAttributeService = specificationAttributeService;
            _catalogSettings = catalogSettings;
        }

        public async Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)>
            Handle(GetSearchProductsQuery request, CancellationToken cancellationToken)
        {
            var filterableSpecificationAttributeOptionIds = new List<string>();

            //validate "categoryIds" parameter
            if (request.CategoryIds != null && request.CategoryIds.Contains(""))
                request.CategoryIds.Remove("");

            //Access control list. Allowed customer groups
            var allowedCustomerGroupsIds = request.Customer.GetCustomerGroupIds();

            #region Search products

            //products
            var query = from p in _productRepository.Table
                        select p;

            var querySpecification = from p in _productRepository.Table
                                     select p;

            //category filtering
            if (request.CategoryIds != null && request.CategoryIds.Any())
            {

                if (request.FeaturedProducts.HasValue)
                {
                    query = query.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId)
                        && y.IsFeaturedProduct == request.FeaturedProducts));
                }
                else
                {
                    query = query.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId)));
                }
            }
            //brand
            if (!string.IsNullOrEmpty(request.BrandId))
            {
                query = query.Where(x => x.BrandId == request.BrandId);
            }
            //collection filtering
            if (!string.IsNullOrEmpty(request.CollectionId))
            {
                if (request.FeaturedProducts.HasValue)
                {
                    query = query.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId
                        && y.IsFeaturedProduct == request.FeaturedProducts));
                }
                else
                {
                    query = query.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId));
                }

            }

            if (!request.OverridePublished.HasValue)
            {
                //process according to "showHidden"
                if (!request.ShowHidden)
                {
                    query = query.Where(p => p.Published);
                }
            }
            else if (request.OverridePublished.Value)
            {
                //published only
                query = query.Where(p => p.Published);
            }
            else if (!request.OverridePublished.Value)
            {
                //unpublished only
                query = query.Where(p => !p.Published);
            }
            if (request.VisibleIndividuallyOnly)
            {
                query = query.Where(p => p.VisibleIndividually);
            }
            if (request.ProductType.HasValue)
            {
                var productTypeId = (int)request.ProductType.Value;
                query = query.Where(p => p.ProductTypeId == (ProductType)productTypeId);
            }
            if (request.ShowOnHomePage.HasValue)
            {
                query = query.Where(p => p.ShowOnHomePage == request.ShowOnHomePage.Value);
            }
            //The function 'CurrentUtcDateTime' is not supported by SQL Server Compact. 
            //That's why we pass the date value
            var nowUtc = DateTime.UtcNow;
            if (request.PriceMin.HasValue)
            {
                query = query.Where(p => p.Price >= request.PriceMin.Value);
            }
            if (request.PriceMax.HasValue)
            {
                //max price
                query = query.Where(p => p.Price <= request.PriceMax.Value);
            }
            if (!request.ShowHidden && !_catalogSettings.IgnoreFilterableAvailableStartEndDateTime)
            {
                query = query.Where(p =>
                    (p.AvailableStartDateTimeUtc == null || p.AvailableStartDateTimeUtc < nowUtc) &&
                    (p.AvailableEndDateTimeUtc == null || p.AvailableEndDateTimeUtc > nowUtc));
            }

            if (request.MarkedAsNewOnly)
            {
                query = query.Where(p => p.MarkAsNew);
                query = query.Where(p =>
                    (!p.MarkAsNewStartDateTimeUtc.HasValue || p.MarkAsNewStartDateTimeUtc.Value < nowUtc) &&
                    (!p.MarkAsNewEndDateTimeUtc.HasValue || p.MarkAsNewEndDateTimeUtc.Value > nowUtc));
            }

            //searching by keyword
            if (!String.IsNullOrWhiteSpace(request.Keywords))
            {

                if (!request.SearchDescriptions)
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(request.Keywords.ToLower())
                        ||
                        p.Locales.Any(x => x.LocaleKey == "Name" && x.LocaleValue != null && x.LocaleValue.ToLower().Contains(request.Keywords.ToLower()))
                        ||
                        (request.SearchSku && p.Sku.ToLower().Contains(request.Keywords.ToLower()))
                        );
                else
                {
                    query = query.Where(p =>
                            (p.Name != null && p.Name.ToLower().Contains(request.Keywords.ToLower()))
                            ||
                            (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(request.Keywords.ToLower()))
                            ||
                            (p.FullDescription != null && p.FullDescription.ToLower().Contains(request.Keywords.ToLower()))
                            ||
                            (p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(request.Keywords.ToLower())))
                            ||
                            (request.SearchSku && p.Sku.ToLower().Contains(request.Keywords.ToLower()))
                            );
                }


            }

            if (!request.ShowHidden && !CommonHelper.IgnoreAcl)
            {
                query = from p in query
                        where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                        select p;
            }

            if (!string.IsNullOrEmpty(request.StoreId) && !CommonHelper.IgnoreStoreLimitations)
            {
                query = query.Where(x => x.Stores.Any(y => y == request.StoreId) || !x.LimitedToStores);

            }
            //vendor filtering
            if (!string.IsNullOrEmpty(request.VendorId))
            {
                query = query.Where(x => x.VendorId == request.VendorId);
            }
            //warehouse filtering
            if (!string.IsNullOrEmpty(request.WarehouseId))
            {
                query = query.Where(x =>
                    (x.UseMultipleWarehouses && x.ProductWarehouseInventory.Any(y => y.WarehouseId == request.WarehouseId))
                    || (!x.UseMultipleWarehouses && x.WarehouseId == request.WarehouseId));
            }

            //tag filtering
            if (!string.IsNullOrEmpty(request.ProductTag))
            {
                query = query.Where(x => x.ProductTags.Any(y => y == request.ProductTag));
            }

            querySpecification = query;

            //search by specs
            if (request.FilteredSpecs != null && request.FilteredSpecs.Any())
            {
                var spec = new HashSet<string>();
                Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
                foreach (string key in request.FilteredSpecs)
                {
                    var specification = await _specificationAttributeService.GetSpecificationAttributeByOptionId(key);
                    if (specification != null)
                    {
                        spec.Add(specification.Id);
                        if (!dictionary.ContainsKey(specification.Id))
                        {
                            //add
                            dictionary.Add(specification.Id, new List<string>());
                            querySpecification = querySpecification.Where(x => x.ProductSpecificationAttributes.Any(y => y.SpecificationAttributeId == specification.Id && y.AllowFiltering));
                        }
                        dictionary[specification.Id].Add(key);
                    }
                }

                foreach (var item in dictionary)
                {
                    query = query.Where(x => x.ProductSpecificationAttributes.Any(y => y.SpecificationAttributeId == item.Key && y.AllowFiltering
                    && item.Value.Contains(y.SpecificationAttributeOptionId)));
                }
            }

            if (request.OrderBy == ProductSortingEnum.Position && request.CategoryIds != null && request.CategoryIds.Any())
            {
                //category position
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderCategory) :
                    query.OrderBy(x => x.DisplayOrderCategory);
            }
            else if (request.OrderBy == ProductSortingEnum.Position && !string.IsNullOrEmpty(request.BrandId))
            {
                //brand position
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderBrand) :
                    query.OrderBy(x => x.DisplayOrderBrand);
            }
            else if (request.OrderBy == ProductSortingEnum.Position && !string.IsNullOrEmpty(request.CollectionId))
            {
                //collection position
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderCollection) :
                    query.OrderBy(x => x.DisplayOrderCollection);
            }
            else if (request.OrderBy == ProductSortingEnum.Position)
            {
                //otherwise sort by name
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.Name) :
                    query.OrderBy(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.NameAsc)
            {
                //Name: A to Z
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.Name) :
                    query.OrderBy(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.NameDesc)
            {
                //Name: Z to A
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Name) :
                    query.OrderByDescending(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.PriceAsc)
            {
                //Price: Low to High
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.Price) :
                    query.OrderBy(x => x.Price);
            }
            else if (request.OrderBy == ProductSortingEnum.PriceDesc)
            {
                //Price: High to Low
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Price) :
                    query.OrderByDescending(x => x.Price);
            }
            else if (request.OrderBy == ProductSortingEnum.CreatedOn)
            {
                //creation date
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.CreatedOnUtc) :
                    query.OrderBy(x => x.CreatedOnUtc);

            }
            else if (request.OrderBy == ProductSortingEnum.OnSale)
            {
                //on sale
                query = query.OrderBy(x => x.OnSale);

                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenBy(x => x.OnSale) :
                    query.OrderBy(x => x.OnSale);
            }
            else if (request.OrderBy == ProductSortingEnum.MostViewed)
            {
                //most viewed
                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Viewed) :
                    query.OrderByDescending(x => x.Viewed);
            }
            else if (request.OrderBy == ProductSortingEnum.BestSellers)
            {
                //best seller
                query = query.OrderByDescending(x => x.Sold);

                query = _catalogSettings.SortingByAvailability ?
                    query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Sold) :
                    query.OrderByDescending(x => x.Sold);
            }

            var products = await PagedList<Product>.Create(query, request.PageIndex, request.PageSize);


            if (request.LoadFilterableSpecificationAttributeOptionIds && !_catalogSettings.IgnoreFilterableSpecAttributeOption)
            {
                IList<string> specyfication = new List<string>();
                var filterSpecExists = querySpecification.Where(x => x.ProductSpecificationAttributes.Any(x=>x.AllowFiltering));

                var qspec = from p in filterSpecExists
                            from item in p.ProductSpecificationAttributes
                            select item;

                var groupQuerySpec = qspec.Where(x=>x.AllowFiltering).GroupBy(x => new { SpecificationAttributeOptionId = x.SpecificationAttributeOptionId }).ToList();
                foreach (var item in groupQuerySpec)
                {
                    specyfication.Add(item.Key.SpecificationAttributeOptionId);
                }

                filterableSpecificationAttributeOptionIds = specyfication.ToList();
            }

            return (products, filterableSpecificationAttributeOptionIds);

            #endregion
        }
    }
}
