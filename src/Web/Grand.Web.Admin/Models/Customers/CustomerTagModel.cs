using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerTagModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Customers.CustomerTags.Fields.Name")]

        public string Name { get; set; }
    }
}