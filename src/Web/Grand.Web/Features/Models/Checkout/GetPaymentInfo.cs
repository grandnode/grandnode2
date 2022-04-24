using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Models.Checkout
{
    public class GetPaymentInfo : IRequest<CheckoutPaymentInfoModel>
    {
        public IPaymentProvider PaymentMethod { get; set; }
    }
}
