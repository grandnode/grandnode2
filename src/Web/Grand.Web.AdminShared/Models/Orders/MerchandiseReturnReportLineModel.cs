using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Orders;

public class MerchandiseReturnReportLineModel : BaseModel
{
    //the reason, product or vendor identifier the line counts
    public string Key { get; set; }

    public string Name { get; set; }

    public int ReturnCount { get; set; }

    public int Quantity { get; set; }

    //by product only: the units sold in the period, and the share of them returned
    public int? SoldQuantity { get; set; }

    public double? ReturnRate { get; set; }
}
