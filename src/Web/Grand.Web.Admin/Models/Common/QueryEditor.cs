
using Grand.Infrastructure.ModelBinding;

namespace Grand.Web.Admin.Models.Common
{
    public partial class QueryEditor
    {
        [GrandResourceDisplayName("Admin.System.Field.QueryEditor")]
        public string Query { get; set; }
    }
}
