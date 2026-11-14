using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Payments;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payments.StripeCheckout.Models;
using Payments.StripeCheckout.Services;

namespace Payments.StripeCheckout.Controllers;

public class PaymentStripeCheckoutController : BasePaymentController
{
    private readonly ILogger<PaymentStripeCheckoutController> _logger;

    private readonly PaymentSettings _paymentSettings;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IStripeCheckoutService _stripeCheckoutService;

    public PaymentStripeCheckoutController(
        ILogger<PaymentStripeCheckoutController> logger,
        IPaymentTransactionService paymentTransactionService,
        PaymentSettings paymentSettings,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings, IStripeCheckoutService stripeCheckoutService)
    {
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

    public IActionResult CancelOrder(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
            return RedirectToRoute("HomePage");

        //the order's page checks who may see it, a guest who came with the order's link too, and answers the same
        //for an order that is not the visitor's and one that does not exist
        return RedirectToRoute("OrderDetails", new { orderId });
    }

    public IActionResult PaymentInfo()
    {
        return View(new PaymentInfo(_stripeCheckoutPaymentSettings.Description));
    }
}