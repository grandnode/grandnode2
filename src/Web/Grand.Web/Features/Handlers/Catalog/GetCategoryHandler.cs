﻿using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCategoryHandler : IRequestHandler<GetCategory, CategoryModel>
    {
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly ICategoryService _categoryService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;

        public GetCategoryHandler(
            IMediator mediator,
            ICacheBase cacheBase,
            ICategoryService categoryService,
            IPictureService pictureService,
            ITranslationService translationService,
            ISpecificationAttributeService specificationAttributeService,
            IHttpContextAccessor httpContextAccessor,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings)
        {
            _mediator = mediator;
            _cacheBase = cacheBase;
            _categoryService = categoryService;
            _pictureService = pictureService;
            _translationService = translationService;
            _specificationAttributeService = specificationAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<CategoryModel> Handle(GetCategory request, CancellationToken cancellationToken)
        {
            var model = request.Category.ToModel(request.Language);
            var customer = request.Customer;
            var storeId = request.Store.Id;
            var languageId = request.Language.Id;
            var currency = request.Currency;

            if (request.Command != null && request.Command.OrderBy == null && request.Category.DefaultSort >= 0)
                request.Command.OrderBy = request.Category.DefaultSort;

            //view/sorting/page size
            var options = await _mediator.Send(new GetViewSortSizeOptions() {
                Command = request.Command,
                PagingFilteringModel = request.Command,
                Language = request.Language,
                AllowCustomersToSelectPageSize = request.Category.AllowCustomersToSelectPageSize,
                PageSizeOptions = request.Category.PageSizeOptions,
                PageSize = request.Category.PageSize
            });
            model.PagingFilteringContext = options.command;

            //price ranges
            //category breadcrumb
            if (_catalogSettings.CategoryBreadcrumbEnabled)
            {
                model.DisplayCategoryBreadcrumb = true;

                string breadcrumbCacheKey = string.Format(CacheKeyConst.CATEGORY_BREADCRUMB_KEY,
                    request.Category.Id,
                    string.Join(",", customer.GetCustomerGroupIds()),
                    storeId,
                    languageId);
                model.CategoryBreadcrumb = await _cacheBase.GetAsync(breadcrumbCacheKey, async () =>
                    (await _categoryService.GetCategoryBreadCrumb(request.Category))
                    .Select(catBr => new CategoryModel {
                        Id = catBr.Id,
                        Name = catBr.GetTranslation(x => x.Name, languageId),
                        SeName = catBr.GetSeName(languageId)
                    })
                    .ToList()
                );
            }

            //subcategories
            var subCategories = new List<CategoryModel.SubCategoryModel>();
            foreach (var x in (await _categoryService.GetAllCategoriesByParentCategoryId(request.Category.Id)).Where(x => !x.HideOnCatalog))
            {
                var subCatModel = new CategoryModel.SubCategoryModel {
                    Id = x.Id,
                    Name = x.GetTranslation(y => y.Name, languageId),
                    SeName = x.GetSeName(languageId),
                    Description = x.GetTranslation(y => y.Description, languageId),
                    Flag = x.Flag,
                    FlagStyle = x.FlagStyle
                };
                //prepare picture model
                var picture = !string.IsNullOrEmpty(x.PictureId) ? await _pictureService.GetPictureById(x.PictureId) : null;
                subCatModel.PictureModel = new PictureModel {
                    Id = x.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                };
                //"title" attribute
                subCatModel.PictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.TitleAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Category.ImageLinkTitleFormat"), x.Name);
                //"alt" attribute
                subCatModel.PictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.AltAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Category.ImageAlternateTextFormat"), x.Name);

                subCategories.Add(subCatModel);
            };

            model.SubCategories = subCategories;

            //featured products
            if (!_catalogSettings.IgnoreFeaturedProducts)
            {
                //We cache a value indicating whether we have featured products
                IPagedList<Product> featuredProducts = null;
                string cacheKey = string.Format(CacheKeyConst.CATEGORY_HAS_FEATURED_PRODUCTS_KEY, request.Category.Id,
                    string.Join(",", customer.GetCustomerGroupIds()), storeId);

                var hasFeaturedProductsCache = await _cacheBase.GetAsync<bool?>(cacheKey, async () =>
                {
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery() {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        CategoryIds = new List<string> { request.Category.Id },
                        Customer = request.Customer,
                        StoreId = storeId,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                    return featuredProducts.Any();
                });

                if (hasFeaturedProductsCache.Value && featuredProducts == null)
                {
                    //cache indicates that the category has featured products
                    featuredProducts = (await _mediator.Send(new GetSearchProductsQuery() {
                        PageSize = _catalogSettings.LimitOfFeaturedProducts,
                        CategoryIds = new List<string> { request.Category.Id },
                        Customer = request.Customer,
                        StoreId = storeId,
                        VisibleIndividuallyOnly = true,
                        FeaturedProducts = true
                    })).products;
                }
                if (featuredProducts != null && featuredProducts.Any())
                {
                    model.FeaturedProducts = (await _mediator.Send(new GetProductOverview() {
                        Products = featuredProducts,
                    })).ToList();
                }
            }


            var categoryIds = new List<string>();
            categoryIds.Add(request.Category.Id);
            if (_catalogSettings.ShowProductsFromSubcategories)
            {
                //include subcategories
                categoryIds.AddRange(await _mediator.Send(new GetChildCategoryIds() { ParentCategoryId = request.Category.Id, Customer = request.Customer, Store = request.Store }));
            }
            //products
            IList<string> alreadyFilteredSpecOptionIds = await model.PagingFilteringContext.SpecificationFilter.GetAlreadyFilteredSpecOptionIds(
                _httpContextAccessor.HttpContext.Request.Query, _specificationAttributeService);
            var products = (await _mediator.Send(new GetSearchProductsQuery() {
                LoadFilterableSpecificationAttributeOptionIds = !_catalogSettings.IgnoreFilterableSpecAttributeOption,
                CategoryIds = categoryIds,
                Customer = request.Customer,
                StoreId = storeId,
                VisibleIndividuallyOnly = true,
                FeaturedProducts = _catalogSettings.IncludeFeaturedProductsInNormalLists ? null : (bool?)false,
                FilteredSpecs = alreadyFilteredSpecOptionIds,
                OrderBy = (ProductSortingEnum)request.Command.OrderBy,
                PageIndex = request.Command.PageNumber - 1,
                PageSize = request.Command.PageSize
            }));

            model.Products = (await _mediator.Send(new GetProductOverview() {
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                Products = products.products,
            })).ToList();

            model.PagingFilteringContext.LoadPagedList(products.products);

            //specs
            await model.PagingFilteringContext.SpecificationFilter.PrepareSpecsFilters(alreadyFilteredSpecOptionIds,
                products.filterableSpecificationAttributeOptionIds,
                _specificationAttributeService, _httpContextAccessor.HttpContext.Request.GetDisplayUrl(), request.Language.Id);

            return model;
        }
    }
}
