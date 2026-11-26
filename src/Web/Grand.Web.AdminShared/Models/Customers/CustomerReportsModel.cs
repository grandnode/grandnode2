using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Customers;

public class CustomerReportsModel : BaseModel
{
    public BestCustomersReportModel BestCustomersByOrderTotal { get; set; } = new();
    public BestCustomersReportModel? BestCustomersByNumberOfOrders { get; set; }
}