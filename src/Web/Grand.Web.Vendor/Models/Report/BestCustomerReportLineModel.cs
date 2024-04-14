using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class BestCustomerReportLineModel : BaseModel
{
    public string CustomerId { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Customers.BestBy.Fields.Customer")]
    public string CustomerName { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Customers.BestBy.Fields.OrderTotal")]
    public string OrderTotal { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Customers.BestBy.Fields.OrderCount")]
    public double OrderCount { get; set; }
}