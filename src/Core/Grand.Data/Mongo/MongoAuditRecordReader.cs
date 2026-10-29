using Grand.Data.Audit;
using Grand.Domain.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Grand.Data.Mongo;

/// <summary>
///     Resolves the database only when a record is read, so it can be registered before installation
/// </summary>
public class MongoAuditRecordReader(Func<IMongoDatabase> database) : IAuditRecordReader
{
    public async Task<IDictionary<string, string>> Read(Type entityType, string id)
    {
        var document = await database().GetCollection<BsonDocument>(entityType.Name)
            .Find(new BsonDocument("_id", id)).FirstOrDefaultAsync();
        return AuditSnapshot.Flatten(document, entityType == typeof(Setting));
    }
}
