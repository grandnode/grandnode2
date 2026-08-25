using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Concrete host subclass of BaseOrderManagementController (ARCH-001 Order consolidation). This class
// supplies Store's DI wiring plus the attributes that used to arrive transitively via
// BaseStoreController - BaseOrderManagementController can't inherit any single host's base controller
// (it's shared across Admin/Store, each with a different [Area]/[Authorize*] pair), so each subclass
// restates its own host's attribute set explicitly, same pattern as CategoryController/ProductController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IMediator mediator,
    IAdminDataScope<Order> scope)
    : BaseOrderManagementController(orderViewModelService, orderService, orderStatusService,
        translationService, contextAccessor, pdfService, mediator, scope);
