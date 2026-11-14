using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Orders;

/// <summary>
///     What the staff found when the goods of a merchandise return came back, posted by "Receive to stock"
/// </summary>
public class ReturnReceiptInspectionModel : BaseModel
{
    //the warehouse the receipt books; empty for the one the items were sold from
    public string WarehouseId { get; set; }

    public IList<LineModel> Lines { get; set; } = new List<LineModel>();

    public class LineModel : BaseModel
    {
        public string ReturnItemId { get; set; }
        public string ProductId { get; set; }
        public int ScrapQuantity { get; set; }
        public string Comment { get; set; }
    }
}
