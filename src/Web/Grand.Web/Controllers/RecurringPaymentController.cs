using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Controllers;

/// <summary>
///     The customer's recurring payments: what a payment provider charges in cycles, and cancelling it
/// </summary>
[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class RecurringPaymentController : BasePublicController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly CustomerSettings _customerSettings;
    private readonly ILogger<RecurringPaymentController> _logger;
    private readonly IMediator _mediator;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly IRequestTranslationService _translationService;

    public RecurringPaymentController(IContextAccessor contextAccessor, IMediator mediator,
        IRecurringPaymentService recurringPaymentService, IRequestTranslationService translationService,
        CustomerSettings customerSettings, ILogger<RecurringPaymentController> logger)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _recurringPaymentService = recurringPaymentService;
        _translationService = translationService;
        _customerSettings = customerSettings;
        _logger = logger;
    }

    //My account / Recurring payments
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> List(OrderPagingModel command)
    {
        if (_customerSettings.HideRecurringPaymentsTab)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetRecurringPayments {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Command = command
        });
        return View(model);
    }
}
