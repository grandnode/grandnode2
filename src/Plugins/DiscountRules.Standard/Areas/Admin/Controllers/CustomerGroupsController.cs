using DiscountRules.CustomerGroups.Models;
using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Discounts;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.CustomerGroups.Controllers
{
    [Area("Admin")]
    [AuthorizeAdmin]
    public class CustomerGroupsController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly IGroupService _groupService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public CustomerGroupsController(
            IDiscountService discountService,
            IGroupService groupService,
            ISettingService settingService,
            IPermissionService permissionService)
        {
            _discountService = discountService;
            _groupService = groupService;
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

            DiscountRule discountRequirement = null;
            if (!String.IsNullOrEmpty(discountRequirementId))
            {
                discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var restrictedToCustomerGroupId = _settingService.GetSettingByKey<RequirementCustomerGroup>(string.Format("DiscountRules.Standard.MustBeAssignedToCustomerGroup-{0}-{1}", discount.Id, !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : ""));

            var model = new RequirementModel();
            model.RequirementId = !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "";
            model.DiscountId = discountId;
            model.CustomerGroupId = restrictedToCustomerGroupId?.CustomerGroupId;
            //customer groups
            model.AvailableCustomerGroups.Add(new SelectListItem { Text = "Select customer group", Value = "" });
            foreach (var cr in await _groupService.GetAllCustomerGroups(showHidden: true))
                model.AvailableCustomerGroups.Add(new SelectListItem { Text = cr.Name, Value = cr.Id.ToString(), Selected = discountRequirement != null && cr.Id == restrictedToCustomerGroupId?.CustomerGroupId });

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesCustomerGroups{0}", !String.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "");

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
            if (!String.IsNullOrEmpty(discountRequirementId))
                discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);

            if (discountRequirement != null)
            {
                //update existing rule
                await _settingService.SetSetting(string.Format("DiscountRules.Standard.MustBeAssignedToCustomerGroup-{0}-{1}", discount.Id, discountRequirement.Id), new RequirementCustomerGroup() { CustomerGroupId = customerGroupId });
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRule {
                    DiscountRequirementRuleSystemName = "DiscountRules.Standard.MustBeAssignedToCustomerGroup"
                };
                discount.DiscountRules.Add(discountRequirement);
                await _discountService.UpdateDiscount(discount);

                await _settingService.SetSetting(string.Format("DiscountRules.Standard.MustBeAssignedToCustomerGroup-{0}-{1}", discount.Id, discountRequirement.Id), new RequirementCustomerGroup() { CustomerGroupId = customerGroupId });
            }
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }
    }
}