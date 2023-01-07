﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetMenuHandler : IRequestHandler<GetMenu, MenuModel>
    {
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly IPageService _pageService;
        private readonly ICacheBase _cacheBase;
        private readonly ICollectionService _collectionService;
        private readonly IPictureService _pictureService;
        private readonly CatalogSettings _catalogSettings;
        private readonly BlogSettings _blogSettings;
        private readonly MenuItemSettings _menuItemSettings;
        private readonly MediaSettings _mediaSettings;

        public GetMenuHandler(
            ICategoryService categoryService,
            IBrandService brandService,
            IPageService pageService,
            ICacheBase cacheBase,
            ICollectionService collectionService,
            IPictureService pictureService,
            CatalogSettings catalogSettings,
            BlogSettings blogSettings,
            MenuItemSettings menuItemSettings,
            MediaSettings mediaSettings)
        {
            _categoryService = categoryService;
            _brandService = brandService;
            _pageService = pageService;
            _cacheBase = cacheBase;
            _collectionService = collectionService;
            _pictureService = pictureService;
            _catalogSettings = catalogSettings;
            _blogSettings = blogSettings;
            _menuItemSettings = menuItemSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<MenuModel> Handle(GetMenu request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.CATEGORIES_BY_MENU,
                request.Language.Id, 
                request.Store.Id, string.Join(",", request.Customer.GetCustomerGroupIds()));
            var cachedCategoriesModel = await _cacheBase.GetAsync(cacheKey, async () => await PrepareCategorySimpleModels(
                language: request.Language,
                allCategories: await _categoryService.GetMenuCategories()));

            //top menu pages
            var now = DateTime.UtcNow;
            var pageModel = (await _pageService.GetAllPages(request.Store.Id))
                .Where(t => t.IncludeInMenu && (!t.StartDateUtc.HasValue || t.StartDateUtc < now) && (!t.EndDateUtc.HasValue || t.EndDateUtc > now))
                .Select(t => new MenuModel.MenuPageModel
                {
                    Id = t.Id,
                    Name = t.GetTranslation(x => x.Title, request.Language.Id),
                    SeName = t.GetSeName(request.Language.Id)
                }).ToList();

            var brandCacheKey = string.Format(CacheKeyConst.BRAND_NAVIGATION_MENU,
                request.Language.Id, request.Store.Id);

            var cachedBrandModel = await _cacheBase.GetAsync(brandCacheKey, async () =>
                    (await _brandService.GetAllBrands(storeId: request.Store.Id))
                    .Where(x => x.IncludeInMenu)
                    .Select(t => new MenuModel.MenuBrandModel
                    {
                        Id = t.Id,
                        Name = t.GetTranslation(x => x.Name, request.Language.Id),
                        Icon = t.Icon,
                        SeName = t.GetSeName(request.Language.Id)
                    })
                    .ToList()
                );

            var collectionCacheKey = string.Format(CacheKeyConst.COLLECTION_NAVIGATION_MENU,
                request.Language.Id, request.Store.Id);

            var cachedCollectionModel = await _cacheBase.GetAsync(collectionCacheKey, async () =>
                    (await _collectionService.GetAllCollections(storeId: request.Store.Id))
                    .Where(x => x.IncludeInMenu)
                    .Select(t => new MenuModel.MenuCollectionModel
                    {
                        Id = t.Id,
                        Name = t.GetTranslation(x => x.Name, request.Language.Id),
                        Icon = t.Icon,
                        SeName = t.GetSeName(request.Language.Id)
                    })
                    .ToList()
                );

            var model = new MenuModel
            {
                Categories = cachedCategoriesModel,
                Brands = cachedBrandModel,
                Pages = pageModel,
                Collections = cachedCollectionModel,
                NewProductsEnabled = _catalogSettings.NewProductsEnabled,
                BlogEnabled = _blogSettings.Enabled,
                DisplayHomePageMenu = _menuItemSettings.DisplayHomePageMenu,
                DisplayNewProductsMenu = _menuItemSettings.DisplayNewProductsMenu,
                DisplaySearchMenu = _menuItemSettings.DisplaySearchMenu,
                DisplayCustomerMenu = _menuItemSettings.DisplayCustomerMenu,
                DisplayBlogMenu = _menuItemSettings.DisplayBlogMenu,
                DisplayContactUsMenu = _menuItemSettings.DisplayContactUsMenu
            };

            return model;

        }

        private async Task<List<CategorySimpleModel>> PrepareCategorySimpleModels(Language language, 
            IEnumerable<Category> allCategories, string rootCategoryId = "",
            bool loadSubCategories = true)
        {
            var result = new List<CategorySimpleModel>();
            if (allCategories == null) return result;
            var categories = allCategories.Where(c => c.ParentCategoryId == rootCategoryId).OrderBy(x => x.DisplayOrder).ToList();
            foreach (var category in categories)
            {
                var picture = await _pictureService.GetPictureById(category.PictureId);
                var categoryModel = new CategorySimpleModel
                {
                    Id = category.Id,
                    Name = category.GetTranslation(x => x.Name, language.Id),
                    SeName = category.GetSeName(language.Id),
                    IncludeInMenu = category.IncludeInMenu,
                    Flag = category.GetTranslation(x => x.Flag, language.Id),
                    FlagStyle = category.FlagStyle,
                    Icon = category.Icon,
                    ImageUrl = await _pictureService.GetPictureUrl(picture, _mediaSettings.CategoryThumbPictureSize),
                    UserFields = category.UserFields
                };
                if (loadSubCategories)
                {
                    var subCategories = await PrepareCategorySimpleModels(language, allCategories, category.Id, true);
                    categoryModel.SubCategories.AddRange(subCategories);
                }
                result.Add(categoryModel);
            }

            return result;
        }
    }
}
