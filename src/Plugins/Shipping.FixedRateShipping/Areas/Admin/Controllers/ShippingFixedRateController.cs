using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Shipping.FixedRateShipping.Models;

namespace Shipping.FixedRateShipping.Areas.Admin.Controllers;

[Area("Admin")]
[AuthorizeAdmin]
public class ShippingFixedRateController : BaseShippingController
{
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IShippingMethodService _shippingMethodService;

    public ShippingFixedRateController(
        IShippingMethodService shippingMethodService,
        ISettingService settingService,
        IPermissionService permissionService)
    {
        _shippingMethodService = shippingMethodService;
        _settingService = settingService;
        _permissionService = permissionService;
    }

    public IActionResult Configure()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Configure(DataSourceRequest command)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageShippingSettings))
            return Content("Access denied");

        var rateModels = new List<FixedShippingRateModel>();
        foreach (var shippingMethod in await _shippingMethodService.GetAllShippingMethods())
            rateModels.Add(new FixedShippingRateModel {
                ShippingMethodId = shippingMethod.Id,
                ShippingMethodName = shippingMethod.Name,
                Rate = GetShippingRate(shippingMethod.Id)
            });

        var gridModel = new DataSourceResult {
            Data = rateModels,
            Total = rateModels.Count
        };
        return Json(gridModel);
    }


    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> ShippingRateUpdate(FixedShippingRateModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageShippingSettings))
            return Content("Access denied");

        var shippingMethodId = model.ShippingMethodId;
        var rate = new FixedShippingRate {
            Rate = model.Rate
        };

        await _settingService.SetSetting(
            $"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{shippingMethodId}", rate);

        return new JsonResult("");
    }

    [NonAction]
    private double GetShippingRate(string shippingMethodId)
    {
        var rate = _settingService.GetSettingByKey<FixedShippingRate>(
            $"ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{shippingMethodId}")?.Rate;
        return rate ?? 0;
    }
}