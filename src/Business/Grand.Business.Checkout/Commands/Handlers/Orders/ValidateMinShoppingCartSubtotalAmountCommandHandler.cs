using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class
    ValidateMinShoppingCartSubtotalAmountCommandHandler : IRequestHandler<ValidateMinShoppingCartSubtotalAmountCommand,
    bool>
{
    private readonly OrderSettings _orderSettings;
    private readonly IShoppingCartSubTotalService _shoppingCartSubTotalService;

    public ValidateMinShoppingCartSubtotalAmountCommandHandler(
        IShoppingCartSubTotalService shoppingCartSubTotalService,
        OrderSettings orderSettings)
    {
        _shoppingCartSubTotalService = shoppingCartSubTotalService;
        _orderSettings = orderSettings;
    }

    public async Task<bool> Handle(ValidateMinShoppingCartSubtotalAmountCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Cart);
        ArgumentNullException.ThrowIfNull(request.Context);

        return await ValidateMinOrderSubtotalAmount(request.Cart, request.Context);
    }

    protected virtual async Task<bool> ValidateMinOrderSubtotalAmount(IList<ShoppingCartItem> cart,
        CalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!cart.Any())
            return false;

        //min order amount sub-total validation
        if (!(_orderSettings.MinOrderSubtotalAmount > 0)) return true;

        //subtotal
        var subTotal = await _shoppingCartSubTotalService.GetShoppingCartSubTotal(cart, context, false);
        return !(subTotal.SubTotalWithoutDiscount < _orderSettings.MinOrderSubtotalAmount);
    }
}