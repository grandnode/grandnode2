using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class
    ValidateMinShoppingCartSubtotalAmountCommandHandler : IRequestHandler<ValidateMinShoppingCartSubtotalAmountCommand,
    bool>
{
    private readonly OrderSettings _orderSettings;
    private readonly IOrderCalculationService _orderTotalCalculationService;

    public ValidateMinShoppingCartSubtotalAmountCommandHandler(
        IOrderCalculationService orderTotalCalculationService,
        OrderSettings orderSettings)
    {
        _orderTotalCalculationService = orderTotalCalculationService;
        _orderSettings = orderSettings;
    }

    public async Task<bool> Handle(ValidateMinShoppingCartSubtotalAmountCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Cart == null)
            throw new ArgumentNullException(nameof(request.Cart));

        if (request.Customer == null)
            throw new ArgumentNullException(nameof(request.Customer));

        return await ValidateMinOrderSubtotalAmount(request.Cart);
    }

    protected virtual async Task<bool> ValidateMinOrderSubtotalAmount(IList<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!cart.Any())
            return false;

        //min order amount sub-total validation
        if (!cart.Any() || !(_orderSettings.MinOrderSubtotalAmount > 0)) return true;
        //subtotal
        var (_, _, subTotalWithoutDiscount, _, _) =
            await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false);
        return !(subTotalWithoutDiscount < _orderSettings.MinOrderSubtotalAmount);
    }
}