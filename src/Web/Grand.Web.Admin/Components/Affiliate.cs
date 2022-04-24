using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Web.Common.Components;
using Grand.Web.Common.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Admin.Models.Affiliates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Checkout.Orders;

namespace Grand.Web.Admin.Components
{
    public class AffiliateViewComponent : BaseAdminViewComponent
    {
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly IOrderStatusService _orderStatusService;

        public AffiliateViewComponent(
            ITranslationService translationService, 
            IPermissionService permissionService,
            IOrderStatusService orderStatusService)
        {
            _translationService = translationService;
            _permissionService = permissionService;
            _orderStatusService = orderStatusService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string affiliateId)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageAffiliates))
                return Content("");

            if (String.IsNullOrEmpty(affiliateId))
                throw new Exception("Affliate ID cannot be empty");

            var model = new AffiliatedOrderListModel();
            model.AffliateId = affiliateId;
            var status = await _orderStatusService.GetAll();

            //order statuses
            model.AvailableOrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //payment statuses
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(HttpContext, false).ToList();
            model.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //shipping statuses
            model.AvailableShippingStatuses = ShippingStatus.Pending.ToSelectList(HttpContext, false).ToList();
            model.AvailableShippingStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            return View(model);
        }
    }
}