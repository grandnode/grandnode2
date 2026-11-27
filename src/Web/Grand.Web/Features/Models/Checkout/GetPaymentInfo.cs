using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Web.Models.Checkout;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Checkout;

public class GetPaymentInfo : IRequest<CheckoutPaymentInfoModel>
{
    public required IPaymentProvider PaymentMethod { get; set; }
}