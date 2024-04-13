using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Discounts;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiscountRules.Standard.Areas.Admin.Controllers;

public class CustomerGroupsController : BaseAdminPluginController
{
    private readonly IDiscountService _discountService;
    private readonly IGroupService _groupService;
    private readonly IPermissionService _permissionService;

    public CustomerGroupsController(
        IDiscountService discountService,
        IGroupService groupService,
        IPermissionService permissionService)
    {
        _discountService = discountService;
        _groupService = groupService;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        DiscountRule discountRequirement = null;
        if (!string.IsNullOrEmpty(discountRequirementId))
        {
            discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                return Content("Failed to load requirement.");
        }

        var model = new RequirementCustomerGroupsModel {
            RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
            DiscountId = discountId,
            CustomerGroupId = discountRequirement?.Metadata
        };

        //customer groups
        model.AvailableCustomerGroups.Add(new SelectListItem { Text = "Select customer group", Value = "" });
        foreach (var cr in await _groupService.GetAllCustomerGroups(showHidden: true))
            model.AvailableCustomerGroups.Add(new SelectListItem {
                Text = cr.Name, Value = cr.Id,
                Selected = discountRequirement != null && cr.Id == discountRequirement?.Metadata
            });

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix =
            $"DiscountRulesCustomerGroups{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(string discountId, string discountRequirementId, string customerGroupId)
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
            discountRequirement.Metadata = customerGroupId;
            await _discountService.UpdateDiscount(discount);
        }
        else
        {
            //save new rule
            discountRequirement = new DiscountRule {
                DiscountRequirementRuleSystemName = "DiscountRules.Standard.MustBeAssignedToCustomerGroup",
                Metadata = customerGroupId
            };
            discount.DiscountRules.Add(discountRequirement);
            await _discountService.UpdateDiscount(discount);
        }

        return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
    }
}