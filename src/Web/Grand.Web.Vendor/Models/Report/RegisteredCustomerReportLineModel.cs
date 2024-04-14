using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Vendor.Models.Report;

public class RegisteredCustomerReportLineModel : BaseModel
{
    [GrandResourceDisplayName("Vendor.Reports.Customers.RegisteredCustomers.Fields.Period")]
    public string Period { get; set; }

    [GrandResourceDisplayName("Vendor.Reports.Customers.RegisteredCustomers.Fields.Customers")]
    public int Customers { get; set; }
}