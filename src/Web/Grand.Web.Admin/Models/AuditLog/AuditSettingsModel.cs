using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.AuditLog;

public class AuditSettingsModel : BaseModel
{
    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.RetentionDays")]
    public int RetentionDays { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.LogStaffSignIns")]
    public bool LogStaffSignIns { get; set; }
}
