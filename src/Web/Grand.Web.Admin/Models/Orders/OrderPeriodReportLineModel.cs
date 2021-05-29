using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;


namespace Grand.Web.Admin.Models.Orders
{
    public partial class OrderPeriodReportLineModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Reports.Period.Name")]
        public string Period { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Period.Count")]
        public int Count { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Period.Amount")]
        public double Amount { get; set; }

    }
}