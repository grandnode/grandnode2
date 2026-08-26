using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BasePaymentTransactionController (ARCH-001 PaymentTransaction
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Admin's DI wiring plus the attributes that used to arrive transitively via BaseAdminController -
// BasePaymentTransactionController can't inherit any single host's base controller (it's shared
// across Admin/Store, each with a different [Area]/[Authorize*] pair), so each subclass restates
// its own host's attribute set explicitly. Same pattern as CollectionController/CategoryController.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class PaymentTransactionController(
    IPaymentTransactionService paymentTransactionService,
    IOrderService orderService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<PaymentTransaction> scope)
    : BasePaymentTransactionController(paymentTransactionService, orderService, translationService,
        dateTimeService, mediator, enumTranslationService, scope);
