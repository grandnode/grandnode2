using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tax.FixedRate.Models;

namespace Tax.FixedRate.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the fixed tax rate provider.
///     Shared (global) rates are managed by the administrator and are read-only here -
///     only a store specific rate can be edited, and editing is always scoped to
///     <see cref="CurrentStoreId" /> (it never modifies the shared/other stores value).
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[AutoValidateAntiforgeryToken]
[PermissionAuthorize(PermissionSystemName.TaxSettings)]
public class TaxFixedRateController : BaseController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ISettingService _settingService;
    private readonly ITaxCategoryService _taxCategoryService;

    public TaxFixedRateController(ITaxCategoryService taxCategoryService,
        ISettingService settingService,
        IContextAccessor contextAccessor)
    {
        _taxCategoryService = taxCategoryService;
        _settingService = settingService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current staff/store-manager is bound to.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult Configure()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> Configure(DataSourceRequest command)
    {
        var taxRateModels = new List<FixedTaxRateModel>();
        foreach (var taxCategory in await _taxCategoryService.GetAllTaxCategories(CurrentStoreId))
        {
            taxRateModels.Add(new FixedTaxRateModel {
                TaxCategoryId = taxCategory.Id,
                TaxCategoryName = taxCategory.Name,
                Rate = await GetTaxRate(taxCategory.Id, CurrentStoreId),
                StoreId = taxCategory.StoreId
            });
        }

        var gridModel = new DataSourceResult {
            Data = taxRateModels,
            Total = taxRateModels.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> TaxRateUpdate(FixedTaxRateModel model)
    {
        if (!await HasStoreRate(model.TaxCategoryId))
            return new JsonResult("");

        await _settingService.SetSetting($"Tax.TaxProvider.FixedRate.TaxCategoryId{model.TaxCategoryId}",
            new FixedTaxRate { Rate = model.Rate }, CurrentStoreId);

        return new JsonResult("");
    }

    /// <summary>
    ///     Whether a store specific override exists for the tax category (store = current store).
    /// </summary>
    [NonAction]
    private async Task<bool> HasStoreRate(string taxCategoryId)
    {
        var taxCategory = await _taxCategoryService.GetTaxCategoryById(taxCategoryId);
        return taxCategory != null && taxCategory.StoreId == CurrentStoreId;
    }

    [NonAction]
    private async Task<double> GetTaxRate(string taxCategoryId, string storeId)
    {
        //store override when present, otherwise the shared/global value (built-in fallback)
        var rate = (await _settingService.GetSettingByKey<FixedTaxRate>(
            $"Tax.TaxProvider.FixedRate.TaxCategoryId{taxCategoryId}", storeId: storeId))?.Rate;
        return rate ?? 0;
    }
}
