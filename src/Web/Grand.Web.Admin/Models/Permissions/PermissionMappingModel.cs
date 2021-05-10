using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Permissions
{
    public partial class PermissionMappingModel : BaseModel
    {
        public PermissionMappingModel()
        {
            AvailablePermissions = new List<PermissionRecordModel>();
            AvailableCustomerGroups = new List<CustomerGroupModel>();
            Allowed = new Dictionary<string, IDictionary<string, bool>>();
        }
        public IList<PermissionRecordModel> AvailablePermissions { get; set; }
        public IList<CustomerGroupModel> AvailableCustomerGroups { get; set; }

        //[permission system name] / [customer group id] / [allowed]
        public IDictionary<string, IDictionary<string, bool>> Allowed { get; set; }
    }
}