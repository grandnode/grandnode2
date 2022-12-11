using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ValidateShoppingCartTotalAmountCommandHandler : IRequestHandler<ValidateShoppingCartTotalAmountCommand, bool>
    {
        private readonly IOrderCalculationService _orderTotalCalculationService;
        private readonly IGroupService _groupService;
        private readonly OrderSettings _orderSettings;

        public ValidateShoppingCartTotalAmountCommandHandler(
            IOrderCalculationService orderTotalCalculationService,
            IGroupService groupService,
            OrderSettings orderSettings)
        {
            _orderTotalCalculationService = orderTotalCalculationService;
            _groupService = groupService;
            _orderSettings = orderSettings;
        }

        public async Task<bool> Handle(ValidateShoppingCartTotalAmountCommand request, CancellationToken cancellationToken)
        {
            if (request.Cart == null)
                throw new ArgumentNullException(nameof(request.Cart));

            if (request.Customer == null)
                throw new ArgumentNullException(nameof(request.Customer));

            var customerGroups = await _groupService.GetAllByIds(request.Customer.Groups.ToArray());
            var minRoles = customerGroups.OrderBy(x => x.MinOrderAmount).FirstOrDefault(x => x.MinOrderAmount.HasValue);
            var minOrderAmount = minRoles?.MinOrderAmount ?? double.MinValue;

            var maxRoles = customerGroups.OrderByDescending(x => x.MaxOrderAmount).FirstOrDefault(x => x.MaxOrderAmount.HasValue);
            var maxOrderAmount = maxRoles?.MaxOrderAmount ?? double.MaxValue;

            if (!request.Cart.Any() || (!(minOrderAmount > 0) && !(maxOrderAmount > 0) &&
                                        !(_orderSettings.MinOrderTotalAmount > 0))) return true;
            var shoppingCartTotalBase = (await _orderTotalCalculationService.GetShoppingCartTotal(request.Cart)).shoppingCartTotal;
            if (shoppingCartTotalBase.HasValue && (shoppingCartTotalBase.Value < minOrderAmount || shoppingCartTotalBase.Value > maxOrderAmount))
                return false;

            return !(_orderSettings.MinOrderTotalAmount > 0) || !shoppingCartTotalBase.HasValue || !(shoppingCartTotalBase.Value < _orderSettings.MinOrderTotalAmount);
        }
    }
}
