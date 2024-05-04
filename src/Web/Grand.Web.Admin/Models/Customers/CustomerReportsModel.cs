using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers;

public class CustomerReportsModel : BaseModel
{
    public BestCustomersReportModel BestCustomersByOrderTotal { get; set; }
    public BestCustomersReportModel BestCustomersByNumberOfOrders { get; set; }
}