using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Orders;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetConfirmOrderHandler : IRequestHandler<GetConfirmOrder, CheckoutConfirmModel>
    {
        private readonly IMediator _mediator;
        private readonly ITranslationService _translationService;
        private readonly OrderSettings _orderSettings;

        public GetConfirmOrderHandler(
            IMediator mediator,
            ITranslationService translationService,
            OrderSettings orderSettings)
        {
            _mediator = mediator;
            _translationService = translationService;
            _orderSettings = orderSettings;
        }

        public async Task<CheckoutConfirmModel> Handle(GetConfirmOrder request, CancellationToken cancellationToken)
        {
            var model = new CheckoutConfirmModel();
            //terms of service
            model.TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage;
            //min order amount validation
            var minOrderTotalAmountOk = await _mediator.Send(new ValidateShoppingCartTotalAmountCommand() { Customer = request.Customer, Cart = request.Cart });
            if (!minOrderTotalAmountOk)
            {
                model.MinOrderTotalWarning = string.Format(_translationService.GetResource("Checkout.MinMaxOrderTotalAmount"));
            }
            return model;
        }
    }
}
