using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
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
        private readonly FullTextSettings _fullTextSettings;

        public GetSearchProductsQueryHandler(
            IRepository<Product> productRepository,
            ISpecificationAttributeService specificationAttributeService,
            CatalogSettings catalogSettings,
            FullTextSettings fullTextSettings)
        {
            _productRepository = productRepository;
            _specificationAttributeService = specificationAttributeService;

            _catalogSettings = catalogSettings;
            _fullTextSettings = fullTextSettings;
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
            var builder = Builders<Product>.Filter;
            var filter = FilterDefinition<Product>.Empty;
            var filterSpecification = FilterDefinition<Product>.Empty;

            //category filtering
            if (request.CategoryIds != null && request.CategoryIds.Any())
            {

                if (request.FeaturedProducts.HasValue)
                {
                    filter &= builder.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId)
                        && y.IsFeaturedProduct == request.FeaturedProducts));
                }
                else
                {
                    filter &= builder.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId)));
                }
            }
            //brand
            if (!string.IsNullOrEmpty(request.BrandId))
            {
                filter &= builder.Where(x => x.BrandId == request.BrandId);
            }
            //collection filtering
            if (!string.IsNullOrEmpty(request.CollectionId))
            {
                if (request.FeaturedProducts.HasValue)
                {
                    filter &= builder.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId
                        && y.IsFeaturedProduct == request.FeaturedProducts));
                }
                else
                {
                    filter &= builder.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId));
                }

            }

            if (!request.OverridePublished.HasValue)
            {
                //process according to "showHidden"
                if (!request.ShowHidden)
                {
                    filter &= builder.Where(p => p.Published);
                }
            }
            else if (request.OverridePublished.Value)
            {
                //published only
                filter &= builder.Where(p => p.Published);
            }
            else if (!request.OverridePublished.Value)
            {
                //unpublished only
                filter &= builder.Where(p => !p.Published);
            }
            if (request.VisibleIndividuallyOnly)
            {
                filter &= builder.Where(p => p.VisibleIndividually);
            }
            if (request.ProductType.HasValue)
            {
                var productTypeId = (int)request.ProductType.Value;
                filter &= builder.Where(p => p.ProductTypeId == (ProductType)productTypeId);
            }

            //The function 'CurrentUtcDateTime' is not supported by SQL Server Compact. 
            //That's why we pass the date value
            var nowUtc = DateTime.UtcNow;
            if (request.PriceMin.HasValue)
            {
                filter &= builder.Where(p => p.Price >= request.PriceMin.Value);
            }
            if (request.PriceMax.HasValue)
            {
                //max price
                filter &= builder.Where(p => p.Price <= request.PriceMax.Value);
            }
            if (!request.ShowHidden && !_catalogSettings.IgnoreFilterableAvailableStartEndDateTime)
            {
                filter &= builder.Where(p =>
                    (p.AvailableStartDateTimeUtc == null || p.AvailableStartDateTimeUtc < nowUtc) &&
                    (p.AvailableEndDateTimeUtc == null || p.AvailableEndDateTimeUtc > nowUtc));
            }

            if (request.MarkedAsNewOnly)
            {
                filter &= builder.Where(p => p.MarkAsNew);
                filter &= builder.Where(p =>
                    (!p.MarkAsNewStartDateTimeUtc.HasValue || p.MarkAsNewStartDateTimeUtc.Value < nowUtc) &&
                    (!p.MarkAsNewEndDateTimeUtc.HasValue || p.MarkAsNewEndDateTimeUtc.Value > nowUtc));
            }

            //searching by keyword
            if (!String.IsNullOrWhiteSpace(request.Keywords))
            {
                if (_fullTextSettings.UseFullTextSearch)
                {
                    request.Keywords = "\"" + request.Keywords + "\"";
                    request.Keywords = request.Keywords.Replace("+", "\" \"");
                    request.Keywords = request.Keywords.Replace(" ", "\" \"");
                    filter &= builder.Text(request.Keywords);
                }
                else
                {
                    if (!request.SearchDescriptions)
                        filter &= builder.Where(p =>
                            p.Name.ToLower().Contains(request.Keywords.ToLower())
                            ||
                            p.Locales.Any(x => x.LocaleKey == "Name" && x.LocaleValue != null && x.LocaleValue.ToLower().Contains(request.Keywords.ToLower()))
                            ||
                            (request.SearchSku && p.Sku.ToLower().Contains(request.Keywords.ToLower()))
                            );
                    else
                    {
                        filter &= builder.Where(p =>
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

            }

            if (!request.ShowHidden && !CommonHelper.IgnoreAcl)
            {
                filter &= (builder.AnyIn(x => x.CustomerGroups, allowedCustomerGroupsIds) | builder.Where(x => !x.LimitedToGroups));
            }

            if (!string.IsNullOrEmpty(request.StoreId) && !CommonHelper.IgnoreStoreLimitations)
            {
                filter &= builder.Where(x => x.Stores.Any(y => y == request.StoreId) || !x.LimitedToStores);

            }
            //vendor filtering
            if (!string.IsNullOrEmpty(request.VendorId))
            {
                filter &= builder.Where(x => x.VendorId == request.VendorId);
            }
            //warehouse filtering
            if (!string.IsNullOrEmpty(request.WarehouseId))
            {
                filter &= (builder.Where(x => x.UseMultipleWarehouses && x.ProductWarehouseInventory.Any(y => y.WarehouseId == request.WarehouseId)) |
                    builder.Where(x => !x.UseMultipleWarehouses && x.WarehouseId == request.WarehouseId));
            }

            //tag filtering
            if (!string.IsNullOrEmpty(request.ProductTag))
            {
                filter &= builder.Where(x => x.ProductTags.Any(y => y == request.ProductTag));
            }

            filterSpecification = filter;

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
                            filterSpecification = filterSpecification & builder.Where(x => x.ProductSpecificationAttributes.Any(y => y.SpecificationAttributeId == specification.Id && y.AllowFiltering));
                        }
                        dictionary[specification.Id].Add(key);
                    }
                }

                foreach (var item in dictionary)
                {
                    filter = filter & builder.Where(x => x.ProductSpecificationAttributes.Any(y => y.SpecificationAttributeId == item.Key && y.AllowFiltering
                    && item.Value.Contains(y.SpecificationAttributeOptionId)));
                }
            }

            SortDefinition<Product> sort = null;
            var builderSort = Builders<Product>.Sort;
            if (_catalogSettings.SortingByAvailability)
            {
                sort = builderSort.Ascending(x => x.LowStock);
            }

            if (request.OrderBy == ProductSortingEnum.Position && request.CategoryIds != null && request.CategoryIds.Any())
            {
                //category position
                sort = sort == null ? builderSort.Ascending(x => x.DisplayOrderCategory) : sort.Ascending(x => x.DisplayOrderCategory);
            }
            else if (request.OrderBy == ProductSortingEnum.Position && !string.IsNullOrEmpty(request.BrandId))
            {
                //brand position
                sort = sort == null ? builderSort.Ascending(x => x.DisplayOrderBrand) : sort.Ascending(x => x.DisplayOrderBrand);
            }
            else if (request.OrderBy == ProductSortingEnum.Position && !string.IsNullOrEmpty(request.CollectionId))
            {
                //collection position
                sort = sort == null ? builderSort.Ascending(x => x.DisplayOrderCollection) : sort.Ascending(x => x.DisplayOrderCollection);
            }
            else if (request.OrderBy == ProductSortingEnum.Position)
            {
                //otherwise sort by name
                sort = sort == null ? builderSort.Ascending(x => x.Name) : sort.Ascending(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.NameAsc)
            {
                //Name: A to Z
                sort = sort == null ? builderSort.Ascending(x => x.Name) : sort.Ascending(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.NameDesc)
            {
                //Name: Z to A
                sort = sort == null ? builderSort.Descending(x => x.Name) : sort.Descending(x => x.Name);
            }
            else if (request.OrderBy == ProductSortingEnum.PriceAsc)
            {
                //Price: Low to High
                sort = sort == null ? builderSort.Ascending(x => x.Price) : sort.Ascending(x => x.Price);
            }
            else if (request.OrderBy == ProductSortingEnum.PriceDesc)
            {
                //Price: High to Low
                sort = sort == null ? builderSort.Descending(x => x.Price) : sort.Descending(x => x.Price);
            }
            else if (request.OrderBy == ProductSortingEnum.CreatedOn)
            {
                //creation date
                sort = sort == null ? builderSort.Ascending(x => x.CreatedOnUtc) : sort.Ascending(x => x.CreatedOnUtc);

            }
            else if (request.OrderBy == ProductSortingEnum.OnSale)
            {
                //on sale
                sort = sort == null ? builderSort.Descending(x => x.OnSale) : sort.Descending(x => x.OnSale);
            }
            else if (request.OrderBy == ProductSortingEnum.MostViewed)
            {
                //most viewed
                sort = sort == null ? builderSort.Descending(x => x.Viewed) : sort.Descending(x => x.Viewed);
            }
            else if (request.OrderBy == ProductSortingEnum.BestSellers)
            {
                //best seller
                sort = sort == null ? builderSort.Descending(x => x.Sold) : sort.Descending(x => x.Sold);
            }



            var products = await PagedList<Product>.Create(_productRepository.Collection, filter, sort, request.PageIndex, request.PageSize);

            if (request.LoadFilterableSpecificationAttributeOptionIds && !_catalogSettings.IgnoreFilterableSpecAttributeOption)
            {
                IList<string> specyfication = new List<string>();
                var filterSpecExists = filterSpecification &
                    builder.Where(x => x.ProductSpecificationAttributes.Count > 0);
                var productSpec = _productRepository.Collection.Find(filterSpecExists).Limit(1);
                if (productSpec != null)
                {
                    var qspec = await _productRepository.Collection
                    .Aggregate()
                    .Match(filterSpecification)
                    .Unwind(x => x.ProductSpecificationAttributes)
                    .Project(new BsonDocument
                        {
                        {"AllowFiltering", "$ProductSpecificationAttributes.AllowFiltering"},
                        {"SpecificationAttributeOptionId", "$ProductSpecificationAttributes.SpecificationAttributeOptionId"}
                        })
                    .Match(new BsonDocument("AllowFiltering", true))
                    .Group(new BsonDocument
                            {
                                        {"_id",
                                            new BsonDocument {
                                                { "SpecificationAttributeOptionId", "$SpecificationAttributeOptionId" },
                                            }
                                        },
                                        {"count", new BsonDocument
                                            {
                                                { "$sum" , 1}
                                            }
                                        }
                            })
                    .ToListAsync();
                    foreach (var item in qspec)
                    {
                        var so = item["_id"]["SpecificationAttributeOptionId"].ToString();
                        specyfication.Add(so);
                    }
                }

                filterableSpecificationAttributeOptionIds = specyfication.ToList();
            }

            return (products, filterableSpecificationAttributeOptionIds);

            #endregion
        }
    }
}
