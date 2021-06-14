using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Shipping.FixedRateShipping.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping.FixedRateShipping.Controllers
{
    [Area("Admin")]
    [AuthorizeAdmin]
    public class ShippingFixedRateController : BaseShippingController
    {
        private readonly IShippingMethodService _shippingMethodService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

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
                rateModels.Add(new FixedShippingRateModel
                {
                    ShippingMethodId = shippingMethod.Id,
                    ShippingMethodName = shippingMethod.Name,
                    Rate = GetShippingRate(shippingMethod.Id)
                });

            var gridModel = new DataSourceResult
            {
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

            string shippingMethodId = model.ShippingMethodId;
            var rate = new FixedShippingRate()
            {
                Rate = model.Rate
            };

            await _settingService.SetSetting(string.Format("ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{0}", shippingMethodId), rate);

            return new JsonResult("");
        }

        [NonAction]
        protected double GetShippingRate(string shippingMethodId)
        {
            var rate = _settingService.GetSettingByKey<FixedShippingRate>(string.Format("ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{0}", shippingMethodId))?.Rate;
            return rate ?? 0;
        }
    }
}