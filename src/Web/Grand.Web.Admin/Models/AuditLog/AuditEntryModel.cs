using Grand.Domain.Audit;

namespace Grand.Web.Admin.Models.AuditLog;

/// <summary>
///     One row of the audit log grid and of the change history popup
/// </summary>
public class AuditEntryRowModel
{
    public string Id { get; set; }
    public string CreatedOn { get; set; }
    public string ActorEmail { get; set; }
    public string Source { get; set; }
    public string Kind { get; set; }
    public string What { get; set; }
    /// <summary>
    ///     What changed, without the values, shortened
    /// </summary>
    public string ChangedFields { get; set; }

    /// <summary>
    ///     Every change with its values in words (see <see cref="AuditChangeText" />), shortened, for the change
    ///     history popup
    /// </summary>
    public string ChangesText { get; set; }
}

public class AuditChangeModel
{
    /// <summary>
    ///     The field in words, e.g. "Specification attributes › Color family › Display order"
    /// </summary>
    public string Label { get; set; }

    public string Path { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
}

/// <summary>
///     The entry detail screen
/// </summary>
public class AuditEntryModel
{
    public AuditEntry Entry { get; set; }
    public string Source { get; set; }
    public string Kind { get; set; }

    /// <summary>
    ///     The store the record belongs to, or "All stores"; null when the record has no store
    /// </summary>
    public string AppliesTo { get; set; }

    public IList<AuditChangeModel> Changes { get; set; } = new List<AuditChangeModel>();
}

/// <summary>
///     The change history popup of one record (EntityId set) or of one entity type
/// </summary>
public class AuditHistoryPopupModel
{
    public string EntityType { get; set; }
    public string EntityId { get; set; }

    /// <summary>
    ///     Comma-separated entity names the popup is limited to (the setting classes of a settings page)
    /// </summary>
    public string EntityNames { get; set; }
}
