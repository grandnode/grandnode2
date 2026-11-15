using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Module.Api.Queries.Models.Orders;

public class GetMerchandiseReturnsQuery : IRequest<IPagedList<MerchandiseReturn>>
{
    public string StoreId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string OrderItemId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public MerchandiseReturnStatus? Status { get; set; }
    public string ReturnShippingProvider { get; set; } = "";
    public string ReasonId { get; set; } = "";
    public bool OpenOnly { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; } = int.MaxValue;
}
