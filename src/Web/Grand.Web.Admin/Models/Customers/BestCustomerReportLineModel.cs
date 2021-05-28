using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class BestCustomerReportLineModel : BaseModel
    {
        public string CustomerId { get; set; }
        [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.Fields.Customer")]
        public string CustomerName { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.Fields.OrderTotal")]
        public string OrderTotal { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Customers.BestBy.Fields.OrderCount")]
        public double OrderCount { get; set; }
    }
}