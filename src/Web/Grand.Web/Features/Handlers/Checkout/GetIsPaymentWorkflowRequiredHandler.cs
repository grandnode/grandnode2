using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.Web.Features.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Handlers.Checkout;

public class GetIsPaymentWorkflowRequiredHandler : IRequestHandler<GetIsPaymentWorkflowRequired, bool>
{
    private readonly IOrderCalculationService _orderTotalCalculationService;
    private readonly PaymentSettings _paymentSettings;

    public GetIsPaymentWorkflowRequiredHandler(IOrderCalculationService orderTotalCalculationService,
        PaymentSettings paymentSettings)
    {
        _orderTotalCalculationService = orderTotalCalculationService;
        _paymentSettings = paymentSettings;
    }

    public async Task<bool> Handle(GetIsPaymentWorkflowRequired request, CancellationToken cancellationToken)
    {
        var result = true;
        //check whether order total equals zero
        var shoppingCartTotalBase =
            (await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart, request.UseLoyaltyPoints))
            .shoppingCartTotal;
        if (shoppingCartTotalBase is 0 && !_paymentSettings.ShowPaymentIfCartIsZero)
            result = false;
        return result;
    }
}