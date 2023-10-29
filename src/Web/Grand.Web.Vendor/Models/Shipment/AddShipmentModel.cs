namespace Grand.Web.Vendor.Models.Shipment;

public class AddShipmentModel
{
    public AddShipmentModel()
    {
        Items = new List<ShipmentItemModel>();
    }
    
    public string OrderId { get; set; }
    public string TrackingNumber { get; set; }
    public string AdminComment { get; set; }
    
    public IList<ShipmentItemModel> Items { get; set; }
    
    public class ShipmentItemModel 
    {
        public string OrderItemId { get; set; }
        public int QuantityToAdd { get; set; }
        public string WarehouseId { get; set; }
    }
}