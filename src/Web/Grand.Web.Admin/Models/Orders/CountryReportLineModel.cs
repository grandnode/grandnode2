using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class CountryReportLineModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Reports.Country.Fields.CountryName")]
        public string CountryName { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Country.Fields.TotalOrders")]
        public int TotalOrders { get; set; }

        [GrandResourceDisplayName("Admin.Reports.Country.Fields.SumOrders")]
        public string SumOrders { get; set; }
    }
}