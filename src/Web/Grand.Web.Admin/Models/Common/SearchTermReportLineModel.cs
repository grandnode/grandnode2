using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Common
{
    public partial class SearchTermReportLineModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.SearchTermReport.Keyword")]
        public string Keyword { get; set; }

        [GrandResourceDisplayName("Admin.SearchTermReport.Count")]
        public int Count { get; set; }
    }
}
