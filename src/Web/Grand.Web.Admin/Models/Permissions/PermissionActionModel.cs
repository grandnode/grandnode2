using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Permissions
{
    public partial class PermissionActionModel : BaseModel
    {
        public PermissionActionModel()
        {
            AvailableActions = new List<string>();
            DeniedActions = new List<string>();
        }
        public string SystemName { get; set; }
        public string PermissionName { get; set; }
        public string CustomerGroupId { get; set; }
        public string CustomerGroupName { get; set; }
        public IList<string> AvailableActions { get; set; }
        public IList<string> DeniedActions { get; set; }
    }
}