using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
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
public class RecurringPaymentController(
    IRecurringPaymentService recurringPaymentService,
    IOrderService orderService,
    ICustomerService customerService,
    IStoreService storeService,
    IPaymentService paymentService,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IAdminDataScope<Order> scope,
    IContextAccessor contextAccessor)
    : BaseRecurringPaymentController(recurringPaymentService, orderService, customerService, storeService,
        paymentService, translationService, enumTranslationService, dateTimeService, mediator, scope,
        contextAccessor);
