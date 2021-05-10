using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class NeverSoldReportLineModel : BaseModel
    {
        public string ProductId { get; set; }
        [GrandResourceDisplayName("Admin.Reports.NeverSold.Fields.Name")]
        public string ProductName { get; set; }
    }
}