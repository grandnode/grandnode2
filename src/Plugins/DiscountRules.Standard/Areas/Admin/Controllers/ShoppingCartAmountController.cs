using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Domain.Discounts;
using Grand.Plugin.DiscountRules.ShoppingCart.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.Standard.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class ShoppingCartAmountController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public ShoppingCartAmountController(IDiscountService discountService,
            ISettingService settingService,
            IPermissionService permissionService)
        {
            _discountService = discountService;
            _settingService = settingService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
                return Content("Access denied");

            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            if (!String.IsNullOrEmpty(discountRequirementId))
            {
                var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var spentAmountRequirement = _settingService.GetSettingByKey<RequirementSpentAmount>(string.Format("DiscountRequirement.ShoppingCart-{0}", !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : ""))?.SpentAmount;

            var model = new RequirementModel();
            model.RequirementId = !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "";
            model.DiscountId = discountId;
            model.SpentAmount = spentAmountRequirement ?? 0;

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesShoppingCart{0}", !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "");

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Configure(string discountId, string discountRequirementId, double spentAmount)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
                return Content("Access denied");

            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRule discountRequirement = null;
            if (!String.IsNullOrEmpty(discountRequirementId))
                discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);

            if (discountRequirement != null)
            {
                //update existing rule
                await _settingService.SetSetting(string.Format("DiscountRequirement.ShoppingCart-{0}", discountRequirement.Id), new RequirementSpentAmount() { SpentAmount = spentAmount });
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRule
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.ShoppingCart"
                };
                discount.DiscountRules.Add(discountRequirement);
                await _discountService.UpdateDiscount(discount);

                await _settingService.SetSetting(string.Format("DiscountRequirement.ShoppingCart-{0}", discountRequirement.Id), new RequirementSpentAmount() { SpentAmount = spentAmount });
            }
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }
    }
}
