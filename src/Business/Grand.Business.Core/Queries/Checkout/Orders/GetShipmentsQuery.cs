using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetShipmentsQuery : IRequest<IQueryable<Shipment>>
{
    public string StoreId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string TrackingNumber { get; set; } = null;
    public bool LoadNotShipped { get; set; } = false;
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
}
