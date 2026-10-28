using Grand.Data.Audit;
using Grand.Domain.Audit;
using MongoDB.Driver;

namespace Grand.Data.Mongo;

/// <summary>
///     Resolves the database only when entries are written, so it can be registered before installation
/// </summary>
public class MongoAuditEntryWriter(Func<IMongoDatabase> database) : IAuditEntryWriter
{
    public Task InsertManyAsync(IReadOnlyCollection<AuditEntry> entries)
    {
        return database().GetCollection<AuditEntry>(nameof(AuditEntry)).InsertManyAsync(entries);
    }
}
