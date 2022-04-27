using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class ReturnBackRedeemedLoyaltyPointsCommandHandler : IRequestHandler<ReturnBackRedeemedLoyaltyPointsCommand, bool>
    {
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly ITranslationService _translationService;

        public ReturnBackRedeemedLoyaltyPointsCommandHandler(
            ILoyaltyPointsService loyaltyPointsService,
            ITranslationService translationService)
        {
            _loyaltyPointsService = loyaltyPointsService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(ReturnBackRedeemedLoyaltyPointsCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            //were some points redeemed when placing an order?
            if (request.Order.RedeemedLoyaltyPoints == 0)
                return false;

            //return back
            await _loyaltyPointsService.AddLoyaltyPointsHistory(request.Order.CustomerId, -request.Order.RedeemedLoyaltyPoints, request.Order.StoreId,
                string.Format(_translationService.GetResource("LoyaltyPoints.Message.ReturnedForOrder"), request.Order.OrderNumber));

            return true;
        }
    }
}
