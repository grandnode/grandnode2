using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ValidateMinShoppingCartSubtotalAmountCommandHandler : IRequestHandler<ValidateMinShoppingCartSubtotalAmountCommand, bool>
    {
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly OrderSettings _orderSettings;

        public ValidateMinShoppingCartSubtotalAmountCommandHandler(
            IOrderCalculationService orderTotalCalculationService,
            OrderSettings orderSettings)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
            _orderSettings = orderSettings;
        }

        public async Task<bool> Handle(ValidateMinShoppingCartSubtotalAmountCommand request, CancellationToken cancellationToken)
        {
            if (request.Cart == null)
                throw new ArgumentNullException(nameof(request.Cart));

            if (request.Customer == null)
                throw new ArgumentNullException(nameof(request.Customer));

            return await ValidateMinOrderSubtotalAmount(request.Cart);
        }

        protected virtual async Task<bool> ValidateMinOrderSubtotalAmount(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (!cart.Any())
                return false;

            //min order amount sub-total validation
            if (cart.Any() && _orderSettings.MinOrderSubtotalAmount > 0)
            {
                //subtotal
                var (_, _, subTotalWithoutDiscount, _, _) = await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false);
                if (subTotalWithoutDiscount < _orderSettings.MinOrderSubtotalAmount)
                    return false;
            }

            return true;
        }

    }
}
