using Grand.Domain.Audit;

namespace Grand.Business.Core.Interfaces.Common.Audit;

/// <summary>
///     Filters of the audit log; null or empty means "any"
/// </summary>
public class AuditEntrySearch
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string ActorEmail { get; set; }
    public AuditSource? Source { get; set; }
    public AuditKind? Kind { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }

    /// <summary>
    ///     Any of these entity names, e.g. the setting classes one settings page saves
    /// </summary>
    public IList<string> EntityNames { get; set; }

    /// <summary>
    ///     Entries made by that store's staff or changing a record of that store
    /// </summary>
    public string StoreId { get; set; }
    public string OperationId { get; set; }
}
