using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class GetPickupPointByIdQueryHandler : IRequestHandler<GetPickupPointById, PickupPoint>
    {
        private readonly IPickupPointService _pickupPointService;
        public GetPickupPointByIdQueryHandler(IPickupPointService pickupPointService)
        {
            _pickupPointService = pickupPointService;
        }
        public async Task<PickupPoint> Handle(GetPickupPointById request, CancellationToken cancellationToken)
        {
            return await _pickupPointService.GetPickupPointById(request.Id);
        }
    }
}
