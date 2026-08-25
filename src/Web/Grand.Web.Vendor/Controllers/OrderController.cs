using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Concrete host subclass of BaseOrderController (ARCH-001 Order consolidation). Vendor gets
// read-only/list/PDF actions only - it inherits BaseOrderController directly, never
// BaseOrderManagementController, so no mutating action exists on this type at all. This class
// supplies Vendor's DI wiring plus the attributes that used to arrive transitively via
// BaseVendorController - BaseOrderController can't inherit any single host's base controller (it's
// shared across Admin/Store/Vendor, each with a different [Area]/[Authorize*] pair), so each subclass
// restates its own host's attribute set explicitly, same pattern as ProductController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IAdminDataScope<Order> scope)
    : BaseOrderController(orderViewModelService, orderService, translationService, contextAccessor,
        pdfService, scope);
