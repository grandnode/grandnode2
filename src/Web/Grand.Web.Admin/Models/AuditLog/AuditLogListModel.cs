using Grand.Domain.Audit;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.AuditLog;

public class AuditLogListModel : BaseModel
{
    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.FromUtc")]
    [UIHint("DateNullable")]
    public DateTime? FromUtc { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.ToUtc")]
    [UIHint("DateNullable")]
    public DateTime? ToUtc { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.ActorEmail")]
    public string ActorEmail { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.Source")]
    public AuditSource? Source { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.Kind")]
    public AuditKind? Kind { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.EntityType")]
    public string EntityType { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.EntityId")]
    public string EntityId { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.StoreId")]
    public string StoreId { get; set; }

    [GrandResourceDisplayName("Admin.System.AuditLog.Fields.OperationId")]
    public string OperationId { get; set; }

    public IList<SelectListItem> AvailableEntityTypes { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableSources { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableKinds { get; set; } = new List<SelectListItem>();
    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public AuditSettingsModel Settings { get; set; } = new();
}
