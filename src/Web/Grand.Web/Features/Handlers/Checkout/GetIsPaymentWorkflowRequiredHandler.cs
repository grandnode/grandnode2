using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Payments;
using Grand.Web.Features.Models.Checkout;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetIsPaymentWorkflowRequiredHandler : IRequestHandler<GetIsPaymentWorkflowRequired, bool>
    {
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly PaymentSettings _paymentSettings;

        public GetIsPaymentWorkflowRequiredHandler(IOrderCalculationService orderTotalCalculationService, PaymentSettings paymentSettings)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
            _paymentSettings = paymentSettings;
        }
        public async Task<bool> Handle(GetIsPaymentWorkflowRequired request, CancellationToken cancellationToken)
        {
            bool result = true;
            //check whether order total equals zero
            double? shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart, useLoyaltyPoints: request.UseLoyaltyPoints)).shoppingCartTotal;
            if (shoppingCartTotalBase.HasValue && shoppingCartTotalBase.Value == 0 && !_paymentSettings.ShowPaymentIfCartIsZero)
                result = false;
            return result;
        }
    }
}
