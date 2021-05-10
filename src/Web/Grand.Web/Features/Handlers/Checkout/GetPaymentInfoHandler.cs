using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetPaymentInfoHandler : IRequestHandler<GetPaymentInfo, CheckoutPaymentInfoModel>
    {

        public async Task<CheckoutPaymentInfoModel> Handle(GetPaymentInfo request, CancellationToken cancellationToken)
        {
            request.PaymentMethod.GetPublicViewComponent(out string viewComponentName);

            var model = new CheckoutPaymentInfoModel
            {
                PaymentViewComponentName = viewComponentName,
            };
            return await Task.FromResult(model);
        }
    }
}
