using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

public class SearchController : BaseVendorController
{
    private readonly AdminSearchSettings _adminSearchSettings;
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;

    public SearchController(ICategoryService categoryService,
        IBrandService brandService, ICollectionService collectionService,
        AdminSearchSettings adminSearchSettings)
    {
        _categoryService = categoryService;
        _brandService = brandService;
        _collectionService = collectionService;
        _adminSearchSettings = adminSearchSettings;
    }

    [HttpGet]
    public async Task<IActionResult> Category(string categoryId, DataSourceRequestFilter model)
    {
        var categories = await _categoryService.GetAllCategories(
            parentId: null,
            categoryName: model.GetNameFilterValue(),
            storeId: "",
            pageIndex: 0,
            pageSize: _adminSearchSettings.CategorySizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(categoryId, categories, async category => await _categoryService.GetFormattedBreadCrumb(category));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Collection(string collectionId, DataSourceRequestFilter model)
    {
        var collections = await _collectionService.GetAllCollections(
            collectionName: model.GetNameFilterValue(),
            storeId: "",
            pageIndex: 0,
            pageSize: _adminSearchSettings.CollectionSizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(collectionId, collections, collection => Task.FromResult(collection.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public async Task<IActionResult> Brand(string brandId, DataSourceRequestFilter model)
    {
        var brands = await _brandService.GetAllBrands(
            brandName: model.GetNameFilterValue(),
            storeId: "",
            pageIndex: 0,
            pageSize: _adminSearchSettings.BrandSizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(brandId, brands, brand => Task.FromResult(brand.Name));
        return Json(gridModel);
    }
}