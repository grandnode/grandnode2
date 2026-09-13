using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BasePaymentTransactionController (ARCH-001 PaymentTransaction
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Store's DI wiring and the attributes that used to arrive transitively via BaseStoreController.
// Same pattern as CollectionController/CategoryController (see those files).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
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
