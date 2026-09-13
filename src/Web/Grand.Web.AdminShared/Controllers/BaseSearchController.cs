using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// ARCH-001: only the Category/Collection/Brand Kendo-autocomplete "picker" sub-resource of
// Admin/Store/Vendor's SearchController is consolidated here - same "consolidate a sub-resource,
// leave the rest duplicated" shape as BaseTaxCategoryController. Admin's own Index (full admin
// command/menu search) and CustomerGroup/Stores/Vendor pickers have no Store/Vendor equivalent and
// stay in Grand.Web.Admin.Controllers.SearchController untouched.
//
// Deliberately does NOT take IAdminDataScope<Category>/<Collection>/<Brand>: those entities' routed
// scopes (RoutedCategoryDataScope etc.) fail closed for the "Vendor" area, because Category/
// Collection/Brand have no Vendor CRUD screen - but this picker sub-resource DOES run under Vendor
// (Vendor's own SearchController already exposes it), so resolving one of those scopes here would
// throw on every Vendor picker call. Instead: Admin's and Vendor's original code both hardcoded
// storeId: "" (no store filter) for these 3 methods; only Store scoped by
// WorkContext.CurrentCustomer.StaffStoreId. That's preserved via the PickerStoreId virtual property
// below (null default = Admin/Vendor's original "" - IsNullOrEmpty("") and IsNullOrEmpty(null) are
// both true in the underlying service filters, confirmed in CategoryService.GetAllCategories), which
// Store's concrete subclass overrides.
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
