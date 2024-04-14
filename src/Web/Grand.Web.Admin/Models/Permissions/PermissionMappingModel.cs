using Grand.Infrastructure.Models;
using Grand.Web.Common.Models;

namespace Grand.Web.Admin.Models.Permissions;

public class PermissionMappingModel : BaseModel
{
    public IList<PermissionRecordModel> AvailablePermissions { get; set; } = new List<PermissionRecordModel>();
    public IList<CustomerGroupModel> AvailableCustomerGroups { get; set; } = new List<CustomerGroupModel>();

    //[permission system name] / [customer group id] / [allowed]
    public IDictionary<string, IDictionary<string, bool>> Allowed { get; set; } =
        new Dictionary<string, IDictionary<string, bool>>();
}