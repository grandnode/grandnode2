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
    public async Task<IDictionary<string, string>> Read(Type entityType, string value, string key = null)
    {
        //a key other than the id may hold a number, stored as one
        BsonValue match = key != null && int.TryParse(value, out var number) ? number : value;
        var document = await database().GetCollection<BsonDocument>(entityType.Name)
            .Find(new BsonDocument(key ?? "_id", match)).FirstOrDefaultAsync();
        return AuditSnapshot.Flatten(document, entityType == typeof(Setting));
    }
}
