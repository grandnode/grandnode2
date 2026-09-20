using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseSearchController(
    ICategoryService categoryService,
    IBrandService brandService,
    ICollectionService collectionService,
    AdminSearchSettings adminSearchSettings)
    : BaseController
{
    protected virtual string PickerStoreId => "";

    [HttpGet]
    public virtual async Task<IActionResult> Category(string categoryId, DataSourceRequestFilter model)
    {
        var categories = await categoryService.GetAllCategories(
            parentId: null,
            categoryName: model.GetNameFilterValue(),
            storeId: PickerStoreId,
            pageIndex: 0,
            pageSize: adminSearchSettings.CategorySizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(categoryId, categories, async category => await categoryService.GetFormattedBreadCrumb(category));
        return Json(gridModel);
    }

    [HttpGet]
    public virtual async Task<IActionResult> Collection(string collectionId, DataSourceRequestFilter model)
    {
        var collections = await collectionService.GetAllCollections(
            collectionName: model.GetNameFilterValue(),
            storeId: PickerStoreId,
            pageIndex: 0,
            pageSize: adminSearchSettings.CollectionSizeLimit,
            showHidden: false
        );

        var gridModel = await DataSourceResultHelper.GetSearchResult(collectionId, collections, collection => Task.FromResult(collection.Name));
        return Json(gridModel);
    }

    [HttpGet]
    public virtual async Task<IActionResult> Brand(string brandId, DataSourceRequestFilter model)
    {
        var brands = await brandService.GetAllBrands(
            model.GetNameFilterValue(),
            storeId: PickerStoreId,
            pageSize: adminSearchSettings.BrandSizeLimit);

        var gridModel = await DataSourceResultHelper.GetSearchResult(brandId, brands, brand => Task.FromResult(brand.Name));
        return Json(gridModel);
    }
}
