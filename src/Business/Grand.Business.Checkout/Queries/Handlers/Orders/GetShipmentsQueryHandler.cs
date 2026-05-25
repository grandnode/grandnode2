using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetShipmentsQueryHandler : IRequestHandler<GetShipmentsQuery, IQueryable<Shipment>>
{
    private readonly IRepository<Shipment> _shipmentRepository;

    public GetShipmentsQueryHandler(IRepository<Shipment> shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public Task<IQueryable<Shipment>> Handle(GetShipmentsQuery request, CancellationToken cancellationToken)
    {
        var query = from s in _shipmentRepository.Table
            select s;

        if (!string.IsNullOrEmpty(request.StoreId))
            query = query.Where(s => s.StoreId == request.StoreId);

        if (!string.IsNullOrEmpty(request.VendorId))
            query = query.Where(s => s.VendorId == request.VendorId);

        if (!string.IsNullOrEmpty(request.WarehouseId))
            query = query.Where(s => s.ShipmentItems.Any(i => i.WarehouseId == request.WarehouseId));

        if (!string.IsNullOrEmpty(request.OrderId))
            query = query.Where(s => s.OrderId == request.OrderId);

        if (!string.IsNullOrEmpty(request.TrackingNumber))
            query = query.Where(s => s.TrackingNumber == request.TrackingNumber);

        if (request.LoadNotShipped)
            query = query.Where(s => s.ShippedDateUtc == null);

        if (request.CreatedFromUtc.HasValue)
            query = query.Where(s => s.CreatedOnUtc >= request.CreatedFromUtc.Value);

        if (request.CreatedToUtc.HasValue)
            query = query.Where(s => s.CreatedOnUtc <= request.CreatedToUtc.Value);

        query = query.OrderByDescending(s => s.CreatedOnUtc);

        return Task.FromResult(query);
    }
}
