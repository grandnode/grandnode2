using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Page;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Startup;
using Grand.Web.Events;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Grand.Web.Common.Localization;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class OrderController : BasePublicController
{
    #region Constructors

    public OrderController(IOrderService orderService,
        IContextAccessor contextAccessor,
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransactionService,
        IRequestTranslationService translationService,
        IGroupService groupService,
        IMediator mediator,
        OrderSettings orderSettings)
    {
        _orderService = orderService;
        _contextAccessor = contextAccessor;
        _paymentService = paymentService;
        _paymentTransactionService = paymentTransactionService;
        _translationService = translationService;
        _groupService = groupService;
        _mediator = mediator;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Fields

    private readonly IOrderService _orderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IRequestTranslationService _translationService;
    private readonly IGroupService _groupService;
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;

    #endregion

    #region Utilities

    /// <summary>
    ///     The order and the customer acting on it: the current customer when it is theirs, else the order's guest
    ///     when the browser holds the grant the link sent to the order's e-mail left; nulls otherwise
    /// </summary>
    private async Task<(Order Order, Customer Customer, bool GuestLink)> CustomerOrder(Order order)
    {
        var (customer, guestLink) = await GuestOrderGrant.OrderCustomer(HttpContext, order,
            _contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore.Id, _groupService);
        return customer != null ? (order, customer, guestLink) : (null, null, false);
    }

    #endregion

    #region Methods

    //My account / Orders
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<ActionResult<OrderPagingModel>> CustomerOrders(OrderPagingModel command)
    {
        var model = await _mediator.Send(new GetCustomerOrderList {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Command = command
        });
        return View(model);
    }

    //My account / Order details page
    [HttpGet]
    public virtual async Task<ActionResult<OrderDetailsModel>> Details(string orderId)
    {
        var (order, customer, guestLink) = await CustomerOrder(await _orderService.GetOrderById(orderId));
        if (order == null)
            return Challenge();

        var model = await _mediator.Send(new GetOrderDetails
            {
                Order = order,
                Customer = customer,
                Language = _contextAccessor.WorkContext.WorkingLanguage
            });
        model.GuestLink = guestLink;
        if (guestLink)
        {
            //the link lets a guest read the order and pay for it, nothing that changes it
            model.UserCanCancelUnpaidOrder = false;
            model.IsReOrderAllowed = false;
            model.ShowAddOrderNote = false;
            model.CanWithdrawOrderApproval = false;
            //the order's returns are reached from here only
            if (GuestOrderGrant.ReturnsAllowed(_orderSettings))
                model.HasMerchandiseReturns = (await _mediator.Send(new GetMerchandiseReturnQuery {
                    StoreId = _contextAccessor.StoreContext.CurrentStore.Id, OrderId = order.Id, PageSize = 1
                })).Any();
        }

        return View(model);
    }

    //My account / Order details page / Withdraw a request the store has not decided yet
    [HttpPost]
    public virtual async Task<IActionResult> WithdrawApproval(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        var withdrawn = await _mediator.Send(new WithdrawOrderApprovalCommand {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            OrderId = orderId
        });

        if (withdrawn)
            Success(_translationService.GetResource("Order.Approval.Withdrawn"));
        else
            Error(_translationService.GetResource("Order.Approval.WithdrawRefused"));

        return RedirectToRoute("OrderDetails", new { orderId });
    }

    //My account / Order details page / Cancel Unpaid Order
    [HttpGet]
    public virtual async Task<IActionResult> CancelOrder(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService)
            || order.PaymentStatusId != PaymentStatus.Pending
            || (order.ShippingStatusId != ShippingStatus.ShippingNotRequired &&
                order.ShippingStatusId != ShippingStatus.Pending)
            || order.OrderStatusId != (int)OrderStatusSystem.Pending
            || !_orderSettings.UserCanCancelUnpaidOrder)

            return Challenge();

        await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = true, NotifyStoreOwner = true });

        return RedirectToRoute("OrderDetails", new { orderId });
    }

    //My account / Order details page / PDF invoice
    [HttpGet]
    public virtual async Task<IActionResult> GetPdfInvoice(string orderId, [FromServices] IPdfService pdfService)
    {
        var (order, _, _) = await CustomerOrder(await _orderService.GetOrderById(orderId));
        if (order == null)
            return Challenge();

        var orders = new List<Order> { order };
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, _contextAccessor.WorkContext.WorkingLanguage.Id,
                cancellationToken: HttpContext.RequestAborted);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"order_{order.Id}.pdf");
    }

    //My account / Order details page / Add order note        
    [HttpPost]
    public virtual async Task<ActionResult<AddOrderNoteModel>> AddOrderNote(AddOrderNoteModel model)
    {
        if (!_orderSettings.AllowCustomerToAddOrderNote)
            return RedirectToRoute("HomePage");

        if (!ModelState.IsValid) return View("AddOrderNote", model);

        var order = await _orderService.GetOrderById(model.OrderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        await _mediator.Send(new InsertOrderNoteCommand {
            Order = order, OrderNote = model, Language = _contextAccessor.WorkContext.WorkingLanguage,
            Host = _contextAccessor.StoreContext.CurrentHost
        });

        //notification
        await _mediator.Publish(new OrderNoteEvent(order, model));

        Notification(NotifyType.Success, _translationService.GetResource("OrderNote.Added"), true);
        return RedirectToRoute("OrderDetails", new { orderId = model.OrderId });
    }

    //My account / Order details page / re-order
    [HttpGet]
    public virtual async Task<IActionResult> ReOrder(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        var warnings = await _mediator.Send(new ReOrderCommand {
            Order = order,
            Language = _contextAccessor.WorkContext.WorkingLanguage
        });
        if (warnings.Any())
            Notification(NotifyType.Error, string.Join(",", warnings), true);

        return RedirectToRoute("ShoppingCart");
    }

    //My account / Order details page / Complete payment
    [HttpPost]
    public virtual async Task<IActionResult> RePostPayment(string orderId)
    {
        var (order, _, _) = await CustomerOrder(await _orderService.GetOrderById(orderId));
        if (order == null)
            return Challenge();

        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);

        if (paymentTransaction == null || !await _paymentService.CanRePostRedirectPayment(paymentTransaction))
            return RedirectToRoute("OrderDetails", new { orderId });

        var redirectUrl = await _paymentService.PostRedirectPayment(paymentTransaction);
        if (!string.IsNullOrEmpty(redirectUrl))
            return Redirect(redirectUrl);
        
        return RedirectToRoute("OrderDetails", new { orderId });
    }

    //My account / Order details page / Shipment details page
    [HttpGet]
    public virtual async Task<ActionResult<ShipmentDetailsModel>> ShipmentDetails(string shipmentId,
        [FromServices] IShipmentService shipmentService)
    {
        var shipment = await shipmentService.GetShipmentById(shipmentId);
        if (shipment == null)
            return Challenge();

        var (order, customer, _) = await CustomerOrder(await _orderService.GetOrderById(shipment.OrderId));
        if (order == null)
            return Challenge();

        var model = await _mediator.Send(new GetShipmentDetails {
            Customer = customer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Order = order,
            Shipment = shipment
        });

        return View(model);
    }

    /// <summary>
    ///     The page where a guest who ordered without an account asks for the link to the order
    /// </summary>
    [HttpGet]
    public virtual IActionResult GuestOrder([FromServices] CaptchaSettings captchaSettings)
    {
        if (!GuestOrderGrant.Allowed(_orderSettings))
            return NotFound();

        return View(new GuestOrderModel { DisplayCaptcha = DisplayGuestCaptcha(captchaSettings) });
    }

    /// <summary>
    ///     Sends the link when the order code and e-mail address match a guest's order. The answer is the same
    ///     whatever was found: it never tells whether such an order exists
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(GrandCommonStartup.GuestOrderRateLimitPolicy)]
    public virtual async Task<IActionResult> GuestOrder(GuestOrderModel model,
        [FromServices] CaptchaSettings captchaSettings)
    {
        if (!GuestOrderGrant.Allowed(_orderSettings))
            //with a body: an empty 404 of a POST is re-executed as a POST of the not-found page
            return NotFound("The page was not found.");

        if (ModelState.IsValid)
        {
            await _mediator.Send(new SendGuestOrderLinkCommand {
                OrderNumberOrCode = model.OrderNumberOrCode,
                Email = model.Email,
                Store = _contextAccessor.StoreContext.CurrentStore,
                Host = _contextAccessor.StoreContext.CurrentHost
            });
            model.Result = _translationService.GetResource("Order.Guest.Sent");
            //the fields render what was posted unless the posted values are dropped as well
            ModelState.Clear();
            model.OrderNumberOrCode = null;
            model.Email = null;
        }

        model.DisplayCaptcha = DisplayGuestCaptcha(captchaSettings);
        return View(model);
    }

    /// <summary>
    ///     The link of the e-mail: leaves the grant for the order in the browser and opens it
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GuestOrderAccess(string token,
        [FromServices] IDataProtectionProvider dataProtectionProvider, [FromServices] ICustomerService customerService)
    {
        if (!GuestOrderGrant.Allowed(_orderSettings))
            return NotFound();

        //the grant serves visitors without an account only: one logged in sees the orders of their account
        if (await _groupService.IsRegistered(_contextAccessor.WorkContext.CurrentCustomer))
        {
            Error(_translationService.GetResource("Order.Guest.LoggedIn"));
            return RedirectToRoute("CustomerOrders");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        var orderId = GuestOrderGrant.LinkOrderId(dataProtectionProvider, token, storeId);
        var order = orderId != null ? await _orderService.GetOrderById(orderId) : null;
        if (await GuestOrderGrant.GuestOfOrder(order, storeId, customerService, _groupService) == null)
        {
            Error(_translationService.GetResource("Order.Guest.InvalidLink"));
            return RedirectToRoute("GuestOrder");
        }

        GuestOrderGrant.Grant(HttpContext, order);
        return RedirectToRoute("OrderDetails", new { orderId = order.Id });
    }

    private static bool DisplayGuestCaptcha(CaptchaSettings captchaSettings)
    {
        return captchaSettings.Enabled && captchaSettings.ShowOnGuestOrderPage;
    }

    //My account / Loyalty points
    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<ActionResult<CustomerLoyaltyPointsModel>> CustomerLoyaltyPoints(
        [FromServices] LoyaltyPointsSettings loyaltyPointsSettings)
    {
        if (!loyaltyPointsSettings.Enabled)
            return RedirectToRoute("CustomerInfo");

        var model = await _mediator.Send(new GetCustomerLoyaltyPoints {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Currency = _contextAccessor.WorkContext.WorkingCurrency
        });
        return View(model);
    }

    #endregion
}