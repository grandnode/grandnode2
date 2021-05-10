using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerActionTypeModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Customer.ActionType.Fields.Name")]
        public string Name { get; set; }
        [GrandResourceDisplayName("Admin.Customer.ActionType.Fields.Enabled")]
        public bool Enabled { get; set; }
    }
}