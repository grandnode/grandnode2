using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Permissions;

public class PermissionActionModel : BaseModel
{
    public string SystemName { get; set; }
    public string PermissionName { get; set; }
    public string CustomerGroupId { get; set; }
    public string CustomerGroupName { get; set; }
    public IList<string> AvailableActions { get; set; } = new List<string>();
    public IList<string> DeniedActions { get; set; } = new List<string>();
}