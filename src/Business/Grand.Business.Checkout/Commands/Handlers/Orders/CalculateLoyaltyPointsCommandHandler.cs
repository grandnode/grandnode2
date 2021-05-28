using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class CalculateLoyaltyPointsCommandHandler : IRequestHandler<CalculateLoyaltyPointsCommand, int>
    {
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;

        public CalculateLoyaltyPointsCommandHandler(LoyaltyPointsSettings loyaltyPointsSettings)
        {
            _loyaltyPointsSettings = loyaltyPointsSettings;
        }

        public async Task<int> Handle(CalculateLoyaltyPointsCommand request, CancellationToken cancellationToken)
        {
            if (!_loyaltyPointsSettings.Enabled)
                return 0;

            if (_loyaltyPointsSettings.PointsForPurchases_Amount <= 0)
                return 0;

            if (request.Customer == null)
                return 0;

            var points = (int)Math.Truncate(request.Amount / _loyaltyPointsSettings.PointsForPurchases_Amount * _loyaltyPointsSettings.PointsForPurchases_Points);

            return await Task.FromResult(points);
        }
    }
}
