using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

/// <summary>
///     Order approvals of a company: a limited sub-account's order waits for an approver's
///     decision. Switched off together with the sub-accounts feature.
/// </summary>
[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class OrderApprovalController : BasePublicController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly CustomerSettings _customerSettings;
    private readonly IMediator _mediator;
    private readonly IRequestTranslationService _translationService;

    public OrderApprovalController(IContextAccessor contextAccessor, IMediator mediator,
        IRequestTranslationService translationService, CustomerSettings customerSettings)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _translationService = translationService;
        _customerSettings = customerSettings;
    }

    /// <summary>
    ///     The sub-accounts switch also switches the approvals off, so every action starts with it
    /// </summary>
    private bool FeatureOff => _customerSettings.HideSubAccountsTab;

    //My account / Order approvals
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> List(OrderPagingModel command)
    {
        if (FeatureOff)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetOrderApprovals {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Command = command
        });
        return View(model);
    }

    //My account / Order approval details; a missing, foreign or hidden request is answered like any other
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Details(string id)
    {
        if (FeatureOff)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetOrderApproval {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Id = id
        });
        if (model == null)
            return RedirectToRoute("CustomerOrderApprovals");

        return View(model);
    }

    [HttpPost]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Decide(string id, bool approve, string comment)
    {
        if (FeatureOff)
            return RedirectToRoute("CustomerInfo");

        var decided = await _mediator.Send(new DecideOrderApprovalCommand {
            Approver = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            ApprovalId = id,
            Approve = approve,
            Comment = comment,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Host = _contextAccessor.StoreContext.CurrentHost
        });

        if (decided)
            Success(_translationService.GetResource(approve
                ? "OrderApproval.Decide.Approved"
                : "OrderApproval.Decide.Rejected"));
        else
            Error(_translationService.GetResource("OrderApproval.Decide.Refused"));

        return RedirectToRoute("CustomerOrderApprovals");
    }

    /// <param name="id">Approval identifier</param>
    /// <param name="toCart">The request was withdrawn from the confirm step, so the buyer returns to the cart</param>
    [HttpPost]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> Withdraw(string id, bool toCart)
    {
        if (FeatureOff)
            return RedirectToRoute("CustomerInfo");

        var withdrawn = await _mediator.Send(new WithdrawOrderApprovalCommand {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            ApprovalId = id
        });

        if (withdrawn)
            Success(_translationService.GetResource("OrderApproval.Withdraw.Success"));
        else
            Error(_translationService.GetResource("OrderApproval.Withdraw.Refused"));

        return RedirectToRoute(toCart ? "ShoppingCart" : "CustomerOrderApprovals");
    }
}
