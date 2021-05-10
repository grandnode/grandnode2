using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Vendors
{
    public partial class VendorListModel : BaseModel
    {
        [GrandResourceDisplayName("Admin.Vendors.List.SearchName")]
        
        public string SearchName { get; set; }
    }
}