using Grand.Data.Audit;
using Grand.Domain.Audit;
using LiteDB;

namespace Grand.Data.LiteDb;

/// <summary>
///     Resolves the database only when entries are written, so it can be registered before installation
/// </summary>
public class LiteDBAuditEntryWriter(Func<LiteDatabase> database) : IAuditEntryWriter
{
    public Task InsertManyAsync(IReadOnlyCollection<AuditEntry> entries)
    {
        database().GetCollection<AuditEntry>(nameof(AuditEntry)).InsertBulk(entries);
        return Task.CompletedTask;
    }
}
