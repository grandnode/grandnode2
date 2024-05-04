namespace Grand.Web.Vendor.Models.Shipment;

public class AddShipmentModel
{
    public string OrderId { get; set; }
    public string TrackingNumber { get; set; }
    public string AdminComment { get; set; }

    public IList<ShipmentItemModel> Items { get; set; } = new List<ShipmentItemModel>();

    public class ShipmentItemModel
    {
        public string OrderItemId { get; set; }
        public int QuantityToAdd { get; set; }
        public string WarehouseId { get; set; }
    }
}