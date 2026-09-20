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
