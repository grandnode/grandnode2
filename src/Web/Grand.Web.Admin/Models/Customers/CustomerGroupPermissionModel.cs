using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Customers
{
    public class CustomerGroupPermissionModel : BaseEntityModel
    {
        public CustomerGroupPermissionModel()
        {
            Actions = new List<string>();
        }
        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Acl.Fields.Name")]
        public string Name { get; set; }

        public string SystemName { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Acl.Fields.Access")]
        public bool Access { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerGroups.Acl.Fields.Actions")]
        public IList<string> Actions { get; set; }
    }
}
