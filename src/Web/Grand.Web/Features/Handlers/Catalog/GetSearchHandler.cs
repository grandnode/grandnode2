﻿using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetSearchHandler : IRequestHandler<GetSearch, SearchModel>
    {
        private readonly ICategoryService _categoryService;
        private readonly ICacheBase _cacheBase;
        private readonly ITranslationService _translationService;
        private readonly ICollectionService _collectionService;
        private readonly IVendorService _vendorService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediator _mediator;
        private readonly ISearchTermService _searchTermService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly CatalogSettings _catalogSettings;
        private readonly VendorSettings _vendorSettings;

        public GetSearchHandler(
            ICategoryService categoryService,
            ICacheBase cacheBase,
            ITranslationService translationService,
            ICollectionService collectionService,
            IVendorService vendorService,
            ICurrencyService currencyService,
            IMediator mediator,
            ISearchTermService searchTermService,
            ISpecificationAttributeService specificationAttributeService,
            IHttpContextAccessor httpContextAccessor,
            CatalogSettings catalogSettings,
            VendorSettings vendorSettings)
        {
            _categoryService = categoryService;
            _cacheBase = cacheBase;
            _translationService = translationService;
            _collectionService = collectionService;
            _vendorService = vendorService;
            _currencyService = currencyService;
            _mediator = mediator;
            _searchTermService = searchTermService;
            _specificationAttributeService = specificationAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _catalogSettings = catalogSettings;
            _vendorSettings = vendorSettings;
        }

        public async Task<SearchModel> Handle(GetSearch request, CancellationToken cancellationToken)
        {
            if (request.Model == null)
                request.Model = new SearchModel();

            var searchTerms = request.Model.q;
            if (searchTerms == null)
                searchTerms = "";
            searchTerms = searchTerms.Trim();

            if (request.Model.Box)
                request.Model.sid = _catalogSettings.SearchByDescription;
            if (request.Model.sid)
                request.Model.adv = true;

            //view/sorting/page size
            var options = await _mediator.Send(new GetViewSortSizeOptions() {
                Command = request.Command,
                PagingFilteringModel = request.Model.PagingFilteringContext,
                Language = request.Language,
                AllowCustomersToSelectPageSize = _catalogSettings.SearchPageAllowCustomersToSelectPageSize,
                PageSizeOptions = _catalogSettings.SearchPagePageSizeOptions,
                PageSize = _catalogSettings.SearchPageProductsPerPage
            });
            request.Model.PagingFilteringContext = options.pagingFilteringModel;
            request.Command = options.command;


            string cacheKey = string.Format(CacheKeyConst.SEARCH_CATEGORIES_MODEL_KEY,
               request.Language.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            var categories = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var categoriesModel = new List<SearchModel.CategoryModel>();
                //all categories
                var allCategories = await _categoryService.GetAllCategories(storeId: request.Store.Id, pageSize: 100);
                foreach (var c in allCategories)
                {
                    //generate full category name (breadcrumb)
                    string categoryBreadcrumb = "";
                    var breadcrumb = _categoryService.GetCategoryBreadCrumb(c, allCategories);
                    for (int i = 0; i <= breadcrumb.Count - 1; i++)
                    {
                        categoryBreadcrumb += breadcrumb[i].GetTranslation(x => x.Name, request.Language.Id);
                        if (i != breadcrumb.Count - 1)
                            categoryBreadcrumb += " >> ";
                    }
                    categoriesModel.Add(new SearchModel.CategoryModel {
                        Id = c.Id,
                        Breadcrumb = categoryBreadcrumb
                    });
                }
                return categoriesModel;
            });
            if (categories.Any())
            {
                //first empty entry
                request.Model.AvailableCategories.Add(new SelectListItem {
                    Value = "",
                    Text = _translationService.GetResource("Common.All")
                });
                //all other categories
                foreach (var c in categories)
                {
                    request.Model.AvailableCategories.Add(new SelectListItem {
                        Value = c.Id.ToString(),
                        Text = c.Breadcrumb,
                        Selected = request.Model.cid == c.Id
                    });
                }
            }

            var collections = await _collectionService.GetAllCollections(pageSize: 100);
            if (collections.Any())
            {
                request.Model.AvailableCollections.Add(new SelectListItem {
                    Value = "",
                    Text = _translationService.GetResource("Common.All")
                });
                foreach (var m in collections)
                    request.Model.AvailableCollections.Add(new SelectListItem {
                        Value = m.Id.ToString(),
                        Text = m.GetTranslation(x => x.Name, request.Language.Id),
                        Selected = request.Model.mid == m.Id
                    });
            }

            request.Model.asv = _vendorSettings.AllowSearchByVendor;
            if (request.Model.asv)
            {
                var vendors = await _vendorService.GetAllVendors(pageSize: 100);
                if (vendors.Any())
                {
                    request.Model.AvailableVendors.Add(new SelectListItem {
                        Value = "",
                        Text = _translationService.GetResource("Common.All")
                    });
                    foreach (var vendor in vendors)
                        request.Model.AvailableVendors.Add(new SelectListItem {
                            Value = vendor.Id.ToString(),
                            Text = vendor.GetTranslation(x => x.Name, request.Language.Id),
                            Selected = request.Model.vid == vendor.Id
                        });
                }
            }

            IPagedList<Product> products = new PagedList<Product>(new List<Product>(), 0, 1);

            if (request.IsSearchTermSpecified)
            {
                if (searchTerms.Length < _catalogSettings.ProductSearchTermMinimumLength)
                {
                    request.Model.Warning = string.Format(_translationService.GetResource("Search.SearchTermMinimumLengthIsNCharacters"), _catalogSettings.ProductSearchTermMinimumLength);
                }
                else
                {
                    var categoryIds = new List<string>();
                    string collectionId = "";
                    double? minPriceConverted = null;
                    double? maxPriceConverted = null;
                    bool searchInDescriptions = false;
                    string vendorId = "";
                    if (request.Model.adv)
                    {
                        //advanced search
                        var categoryId = request.Model.cid;
                        if (!String.IsNullOrEmpty(categoryId))
                        {
                            categoryIds.Add(categoryId);
                            //include subcategories
                            categoryIds.AddRange(await _mediator.Send(new GetChildCategoryIds() { ParentCategoryId = categoryId, Customer = request.Customer, Store = request.Store }));
                        }
                        collectionId = request.Model.mid;

                        //min price
                        if (!string.IsNullOrEmpty(request.Model.pf))
                        {
                            double minPrice;
                            if (double.TryParse(request.Model.pf, out minPrice))
                                minPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrency(minPrice, request.Currency);
                        }
                        //max price
                        if (!string.IsNullOrEmpty(request.Model.pt))
                        {
                            double maxPrice;
                            if (double.TryParse(request.Model.pt, out maxPrice))
                                maxPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrency(maxPrice, request.Currency);
                        }

                        searchInDescriptions = request.Model.sid;
                        if (request.Model.asv)
                            vendorId = request.Model.vid;
                    }

                    var searchInProductTags = searchInDescriptions;

                    IList<string> alreadyFilteredSpecOptionIds = await request.Model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds
                        (_httpContextAccessor.HttpContext.Request.Query, _specificationAttributeService);

                    //products
                    var searchproducts = (await _mediator.Send(new GetSearchProductsQuery() {
                        LoadFilterableSpecificationAttributeOptionIds = !_catalogSettings.IgnoreFilterableSpecAttributeOption,
                        CategoryIds = categoryIds,
                        CollectionId = collectionId,
                        Customer = request.Customer,
                        StoreId = request.Store.Id,
                        VisibleIndividuallyOnly = true,
                        PriceMin = minPriceConverted,
                        PriceMax = maxPriceConverted,
                        Keywords = searchTerms,
                        SearchDescriptions = searchInDescriptions,
                        SearchSku = searchInDescriptions,
                        SearchProductTags = searchInProductTags,
                        FilteredSpecs = alreadyFilteredSpecOptionIds,
                        LanguageId = request.Language.Id,
                        OrderBy = (ProductSortingEnum)request.Command.OrderBy,
                        PageIndex = request.Command.PageNumber - 1,
                        PageSize = request.Command.PageSize,
                        VendorId = vendorId
                    }));

                    request.Model.Products = (await _mediator.Send(new GetProductOverview() {
                        Products = searchproducts.products,
                        PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages
                    })).ToList();

                    request.Model.PagingFilteringContext.LoadPagedList(searchproducts.products);

                    //specs
                    await request.Model.PagingFilteringContext.SpecificationFilter.PrepareSpecsFilters(alreadyFilteredSpecOptionIds,
                        searchproducts.filterableSpecificationAttributeOptionIds,
                        _specificationAttributeService, _httpContextAccessor.HttpContext.Request.GetDisplayUrl(), request.Language.Id);

                    request.Model.NoResults = !request.Model.Products.Any();

                    //search term statistics
                    if (!String.IsNullOrEmpty(searchTerms))
                    {
                        var searchTerm = await _searchTermService.GetSearchTermByKeyword(searchTerms, request.Store.Id);
                        if (searchTerm != null)
                        {
                            searchTerm.Count++;
                            await _searchTermService.UpdateSearchTerm(searchTerm);
                        }
                        else
                        {
                            searchTerm = new SearchTerm {
                                Keyword = searchTerms,
                                StoreId = request.Store.Id,
                                Count = 1
                            };
                            await _searchTermService.InsertSearchTerm(searchTerm);
                        }
                    }
                }
            }

            return request.Model;
        }
    }
}
