using Grand.Domain.Audit;

namespace Grand.Data.Audit;

/// <summary>
///     Writes audit entries in one batch, outside the audited repositories
/// </summary>
public interface IAuditEntryWriter
{
    Task InsertManyAsync(IReadOnlyCollection<AuditEntry> entries);
}
