#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain.Tax;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// Holds only the TaxCategory CRUD region of TaxController — Providers/Settings are not entity-
// shaped (no IAdminDataScope<TEntity> to apply) and stay duplicated in each host's own
// TaxController (see the design spec). [PermissionAuthorize] is safe on the base itself (it's
// not host-specific, unlike [Area]/[AuthorizeAdmin]/[AuthorizeStore]/[AuthorizeMenu], which every
// concrete subclass must restate individually).
[PermissionAuthorize(PermissionSystemName.TaxSettings)]
[AutoValidateAntiforgeryToken]
public abstract class BaseTaxCategoryController(
    ITaxCategoryService taxCategoryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<TaxCategory> scope)
    : BaseController
{
    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public virtual async Task<IActionResult> Categories(DataSourceRequest command)
    {
        var categories = await taxCategoryService.GetAllTaxCategories(scope.DefaultStoreId ?? "");

        Dictionary<string, string>? storeMap = null;
        if (scope.ShowStoreSelector)
            storeMap = (await storeService.GetAllStores()).ToDictionary(s => s.Id, s => s.Shortcut);

        var categoriesModel = categories
            .Select(x => {
                var m = x.ToModel();
                m.StoreName = storeMap is null
                    ? string.Empty
                    : !string.IsNullOrEmpty(x.StoreId) && storeMap.TryGetValue(x.StoreId, out var name)
                        ? name
                        : translationService.GetResource("Admin.Common.All");
                return m;
            })
            .ToList();

        var gridModel = new DataSourceResult {
            Data = categoriesModel,
            Total = categoriesModel.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public virtual async Task<IActionResult> CategoryUpdate(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var taxCategory = await taxCategoryService.GetTaxCategoryById(model.Id);
        if (taxCategory == null || !await scope.HasAccess(taxCategory))
            return new JsonResult("");

        taxCategory = model.ToEntity(taxCategory);
        if (scope.DefaultStoreId is not null)
            taxCategory.StoreId = scope.DefaultStoreId;
        await taxCategoryService.UpdateTaxCategory(taxCategory);

        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public virtual async Task<IActionResult> CategoryAdd(TaxCategoryModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var taxCategory = new TaxCategory();
        taxCategory = model.ToEntity(taxCategory);
        if (scope.DefaultStoreId is not null)
            taxCategory.StoreId = scope.DefaultStoreId;
        await taxCategoryService.InsertTaxCategory(taxCategory);

        return new JsonResult("");
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public virtual async Task<IActionResult> CategoryDelete(string id)
    {
        var taxCategory = await taxCategoryService.GetTaxCategoryById(id);
        if (taxCategory == null || !await scope.HasAccess(taxCategory))
            return new JsonResult("");
        await taxCategoryService.DeleteTaxCategory(taxCategory);

        return new JsonResult("");
    }
}
