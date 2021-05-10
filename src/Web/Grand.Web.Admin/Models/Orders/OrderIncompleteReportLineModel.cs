using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class OrderIncompleteReportLineModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Reports.Incomplete.Item")]
        public string Item { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Incomplete.Total")]
        public string Total { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Incomplete.Count")]
        public int Count { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Incomplete.View")]
        public string ViewLink { get; set; }
    }
}
