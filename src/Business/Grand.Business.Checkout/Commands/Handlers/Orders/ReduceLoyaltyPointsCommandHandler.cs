﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ReduceLoyaltyPointsCommandHandler : IRequestHandler<ReduceLoyaltyPointsCommand, bool>
    {
        private readonly ICustomerService _customerService;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly ICurrencyService _currencyService;

        public ReduceLoyaltyPointsCommandHandler(
            ICustomerService customerService,
            ILoyaltyPointsService loyaltyPointsService,
            IMediator mediator,
            IOrderService orderService,
            ITranslationService translationService,
            ICurrencyService currencyService)
        {
            _customerService = customerService;
            _loyaltyPointsService = loyaltyPointsService;
            _mediator = mediator;
            _orderService = orderService;
            _translationService = translationService;
            _currencyService = currencyService;
        }

        public async Task<bool> Handle(ReduceLoyaltyPointsCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            var customer = await _customerService.GetCustomerById(request.Order.CustomerId);

            var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);

            var amount = await _currencyService.ConvertToPrimaryStoreCurrency(request.Order.OrderTotal - request.Order.OrderShippingInclTax, currency);

            var points = await _mediator.Send(new CalculateLoyaltyPointsCommand() { Customer = customer, Amount = amount }, cancellationToken);
            if (points <= 0)
                return false;

            if (request.Order.CalcLoyaltyPoints < points)
                return false;

            //reduce loyalty points
            await _loyaltyPointsService.AddLoyaltyPointsHistory(customer.Id, -points, request.Order.StoreId,
                string.Format(_translationService.GetResource("LoyaltyPoints.Message.ReducedForOrder"), request.Order.OrderNumber));

            request.Order.CalcLoyaltyPoints -= points;
            //assign to order
            await _orderService.UpdateOrder(request.Order);

            return true;
        }
    }
}
