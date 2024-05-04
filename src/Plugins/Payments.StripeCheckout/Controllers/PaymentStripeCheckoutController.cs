using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payments.StripeCheckout.Models;
using Payments.StripeCheckout.Services;

namespace Payments.StripeCheckout.Controllers;

public class PaymentStripeCheckoutController : BasePaymentController
{
    private readonly ILogger<PaymentStripeCheckoutController> _logger;
    private readonly IOrderService _orderService;

    private readonly PaymentSettings _paymentSettings;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IStripeCheckoutService _stripeCheckoutService;
    private readonly IWorkContext _workContext;

    public PaymentStripeCheckoutController(
        IWorkContext workContext,
        IOrderService orderService,
        ILogger<PaymentStripeCheckoutController> logger,
        IPaymentTransactionService paymentTransactionService,
        PaymentSettings paymentSettings,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings, IStripeCheckoutService stripeCheckoutService)
    {
        _workContext = workContext;
        _orderService = orderService;
        _logger = logger;
        _paymentTransactionService = paymentTransactionService;
        _paymentSettings = paymentSettings;
        _stripeCheckoutPaymentSettings = stripeCheckoutPaymentSettings;
        _stripeCheckoutService = stripeCheckoutService;
    }

    [HttpPost]
    public async Task<IActionResult> WebHook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        try
        {
            await _stripeCheckoutService.WebHookProcessPayment(Request.Headers["Stripe-Signature"], json);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public async Task<IActionResult> CancelOrder(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order != null && order.CustomerId == _workContext.CurrentCustomer.Id)
            return RedirectToRoute("OrderDetails", new { orderId = order.Id });

        return RedirectToRoute("HomePage");
    }

    public IActionResult PaymentInfo()
    {
        return View(new PaymentInfo(_stripeCheckoutPaymentSettings.Description));
    }
}