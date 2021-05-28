using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class SalesEmployeeModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.Email")]
        public string Email { get; set; }

        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.Phone")]
        public string Phone { get; set; }

        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.Active")]
        public bool Active { get; set; }

        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.Commission")]
        public double? Commission { get; set; }

        [GrandResourceDisplayName("Admin.Customers.SalesEmployee.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

    }
}