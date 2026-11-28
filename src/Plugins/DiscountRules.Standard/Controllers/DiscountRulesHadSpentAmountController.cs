using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Domain.Permissions;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace DiscountRules.Standard.Controllers;

public class DiscountRulesHadSpentAmountController : BaseDiscountRulePluginController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly IDiscountService _discountService;
    private readonly IRequestPermissionService _permissionService;

    public DiscountRulesHadSpentAmountController(
        IDiscountService discountService,
        IContextAccessor contextAccessor,
        IRequestPermissionService permissionService)
    {
        _discountService = discountService;
        _contextAccessor = contextAccessor;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        if (!CanView(discount, _contextAccessor.WorkContext.CurrentCustomer))
            return Content("Access denied");

        double spentAmountRequirement = 0;
        if (!string.IsNullOrEmpty(discountRequirementId))
        {
            var discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                return Content("Failed to load requirement.");

            spentAmountRequirement = Convert.ToDouble(discountRequirement.Metadata);
        }

        var model = new RequirementSpentAmountModel {
            RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
            DiscountId = discountId,
            SpentAmount = spentAmountRequirement
        };

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix =
            $"DiscountRulesHadSpentAmount{discount.Id}-{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

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

        if (!CanManage(discount, _contextAccessor.WorkContext.CurrentCustomer))
            return Content("Access denied");

        DiscountRule? discountRequirement = null;
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
            discountRequirement = new DiscountRule {
                DiscountRequirementRuleSystemName = "DiscountRules.Standard.HadSpentAmount",
                Metadata = spentAmount.ToString(CultureInfo.InvariantCulture)
            };
            discount.DiscountRules.Add(discountRequirement);
            await _discountService.UpdateDiscount(discount);
        }

        return new JsonResult(new { Result = true, NewRequirementId = discountRequirement.Id });
    }
}