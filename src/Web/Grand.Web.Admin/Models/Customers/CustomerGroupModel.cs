using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerGroupModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.FreeShipping")]

        public bool FreeShipping { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.TaxExempt")]
        public bool TaxExempt { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.Active")]
        public bool Active { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.IsSystem")]
        public bool IsSystem { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.EnablePasswordLifetime")]
        public bool EnablePasswordLifetime { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.MinOrderAmount")]
        [UIHint("DoubleNullable")]
        public double? MinOrderAmount { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Fields.MaxOrderAmount")]
        [UIHint("DoubleNullable")]
        public double? MaxOrderAmount { get; set; }

    }
}