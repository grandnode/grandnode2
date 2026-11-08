namespace Grand.Web.AdminShared.Models.Orders;

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

        /// <summary>
        ///     A serial-tracked product: the numbers scanned, one a line
        /// </summary>
        public string SerialNumbersText { get; set; }

        /// <summary>
        ///     A batch-tracked product: what each batch of each warehouse gives
        /// </summary>
        public IList<BatchQuantityModel> Batches { get; set; } = new List<BatchQuantityModel>();
    }

    public class BatchQuantityModel
    {
        public string WarehouseId { get; set; }
        public string Number { get; set; }
        public int Quantity { get; set; }
    }
}