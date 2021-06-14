using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tax.FixedRate.Models;

namespace Tax.FixedRate.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.TaxSettings)]
    public class TaxFixedRateController : BasePluginController
    {
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ISettingService _settingService;

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
            string taxCategoryId = model.TaxCategoryId;
            double rate = model.Rate;

            await _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", taxCategoryId), new FixedTaxRate() { Rate = rate });

            return new JsonResult("");
        }

        [NonAction]
        protected double GetTaxRate(string taxCategoryId)
        {
            var rate = _settingService.GetSettingByKey<FixedTaxRate>(string.Format("Tax.TaxProvider.FixedRate.TaxCategoryId{0}", taxCategoryId))?.Rate;
            return rate ?? 0;
        }
    }
}
