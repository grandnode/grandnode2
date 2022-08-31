using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Domain.Discounts;
using Microsoft.AspNetCore.Mvc;
using DiscountRules.Standard.HadSpentAmount.Models;

namespace DiscountRules.Standard.HadSpentAmount.Controllers
{
    [Area("Admin")]
    [AuthorizeAdmin]
    public class HadSpentAmountController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;

        public HadSpentAmountController(
            IDiscountService discountService,
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
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesHadSpentAmount{0}-{1}", discount.Id, !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "");

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
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
                discountRequirement.Metadata = spentAmount.ToString();
                await _discountService.UpdateDiscount(discount);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRule {
                    DiscountRequirementRuleSystemName = "DiscountRules.Standard.HadSpentAmount",
                    Metadata = spentAmount.ToString()
                };
                discount.DiscountRules.Add(discountRequirement);
                await _discountService.UpdateDiscount(discount);
            }
            return new JsonResult(new { Result = true, NewRequirementId = discountRequirement.Id });
        }

    }
}