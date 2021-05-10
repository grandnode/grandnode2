using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Shipping;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
