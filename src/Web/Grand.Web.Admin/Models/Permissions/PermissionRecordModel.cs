using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Permissions
{
    public partial class PermissionRecordModel : BaseModel
    {
        public string Name { get; set; }
        public string SystemName { get; set; }
        public string Area { get; set; }
        public bool Actions { get; set; }
        public string Category { get; set; }
    }
}