using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetMerchandiseReturnQuery : IRequest<IPagedList<MerchandiseReturn>>
{
    public string StoreId { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string OrderItemId { get; set; } = "";
    public MerchandiseReturnStatus? Rs { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc { get; set; } = null;

    //the system name of the return shipping provider the customer chose
    public string ReturnShippingProvider { get; set; } = "";

    //a return with an item of this reason
    public string ReasonId { get; set; } = "";

    //only the returns still being handled: pending, accepted or received
    public bool OpenOnly { get; set; }

    /// <summary>Only the returns of this sales employee's orders.</summary>
    public string SalesEmployeeId { get; set; } = "";
}