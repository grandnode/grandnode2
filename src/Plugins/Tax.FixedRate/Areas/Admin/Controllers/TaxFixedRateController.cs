using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tax.FixedRate.Models;

namespace Tax.FixedRate.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.TaxSettings)]
public class TaxFixedRateController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;
    private readonly ITaxCategoryService _taxCategoryService;

    public TaxFixedRateController(ITaxCategoryService taxCategoryService, ISettingService settingService)
    {
        _taxCategoryService = taxCategoryService;
        _settingService = settingService;
    }


    public IActionResult Configure()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Configure(DataSourceRequest command)
    {
        var taxRateModels = new List<FixedTaxRateModel>();
        foreach (var taxCategory in await _taxCategoryService.GetAllTaxCategories())
            taxRateModels.Add(new FixedTaxRateModel {
                TaxCategoryId = taxCategory.Id,
                TaxCategoryName = taxCategory.Name,
                Rate = GetTaxRate(taxCategory.Id)
            });

        var gridModel = new DataSourceResult {
            Data = taxRateModels,
            Total = taxRateModels.Count
        };
        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> TaxRateUpdate(FixedTaxRateModel model)
    {
        var taxCategoryId = model.TaxCategoryId;
        var rate = model.Rate;

        await _settingService.SetSetting($"Tax.TaxProvider.FixedRate.TaxCategoryId{taxCategoryId}",
            new FixedTaxRate { Rate = rate });

        return new JsonResult("");
    }

    [NonAction]
    private double GetTaxRate(string taxCategoryId)
    {
        var rate = _settingService.GetSettingByKey<FixedTaxRate>(
            $"Tax.TaxProvider.FixedRate.TaxCategoryId{taxCategoryId}")?.Rate;
        return rate ?? 0;
    }
}