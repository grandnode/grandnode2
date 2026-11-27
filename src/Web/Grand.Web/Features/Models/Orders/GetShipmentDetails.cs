using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetShipmentDetails : IRequest<ShipmentDetailsModel>
{
    public required Shipment Shipment { get; set; }
    public required Order Order { get; set; }
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
}