using Grand.Domain.Orders;

namespace Payments.StripeCheckout.Services;

public interface IStripeCheckoutService
{
    Task<string> CreateRedirectUrl(Order order);
    Task<bool> WebHookProcessPayment(string stripeSignature, string json);
}