using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Domain.Discounts;
using Grand.Plugin.DiscountRules.ShoppingCart.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace DiscountRules.Standard.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class ShoppingCartAmountController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;

        public ShoppingCartAmountController(IDiscountService discountService,
            IPermissionService permissionService)
        {
            _discountService = discountService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
                return Content("Access denied");

            var discount = await _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            double spentAmountRequirement = 0;
            if (!string.IsNullOrEmpty(discountRequirementId))
            {
                var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");

                spentAmountRequirement = Convert.ToDouble(discountRequirement.Metadata);
            }

            var model = new RequirementModel {
                RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
                DiscountId = discountId,
                SpentAmount = spentAmountRequirement
            };

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix =
                $"DiscountRulesShoppingCart{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

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
            if (!string.IsNullOrEmpty(discountRequirementId))
                discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);

            if (discountRequirement != null)
            {
                //update existing rule
                discountRequirement.Metadata = spentAmount.ToString(CultureInfo.InvariantCulture);
                await _discountService.UpdateDiscount(discount);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRule
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.ShoppingCart",
                    Metadata = spentAmount.ToString(CultureInfo.InvariantCulture)
                };
                discount.DiscountRules.Add(discountRequirement);
                await _discountService.UpdateDiscount(discount);
            }
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }
    }
}
