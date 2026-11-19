using Grand.Domain.Orders;
using Grand.Domain.Payments;

namespace Payments.StripeCheckout.Services;

public interface IStripeCheckoutService
{
    Task<string> CreateRedirectUrl(Order order);
    Task<IList<string>> Refund(PaymentTransaction paymentTransaction, double? amount);
    Task<bool> WebHookProcessPayment(string stripeSignature, string json);
}