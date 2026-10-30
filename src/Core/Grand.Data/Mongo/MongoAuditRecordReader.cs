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
    public async Task<IDictionary<string, string>> Read(Type entityType, string value, string key = null,
        string field = null)
    {
        var find = database().GetCollection<BsonDocument>(entityType.Name)
            .Find(new BsonDocument(key ?? "_id", Match(value, key)));
        var document = field == null
            ? await find.FirstOrDefaultAsync()
            : await find.Project(Builders<BsonDocument>.Projection.Include(field)).FirstOrDefaultAsync();
        return AuditSnapshot.Flatten(document, entityType == typeof(Setting));
    }

    public async Task<IDictionary<string, IDictionary<string, string>>> ReadMany(Type entityType,
        IReadOnlyCollection<string> values, string key = null, string field = null)
    {
        var name = key ?? "_id";
        var find = database().GetCollection<BsonDocument>(entityType.Name)
            .Find(Builders<BsonDocument>.Filter.In(name, values.Select(v => Match(v, key))));
        //the key stays in the projection: it tells which value a record answers (the id always does)
        var documents = field == null
            ? await find.ToListAsync()
            : await find.Project(key == null || key == field
                ? Builders<BsonDocument>.Projection.Include(field)
                : Builders<BsonDocument>.Projection.Include(field).Include(key)).ToListAsync();

        var records = new Dictionary<string, IDictionary<string, string>>();
        foreach (var document in documents)
            records.TryAdd(document[name].ToString()!, AuditSnapshot.Flatten(document, entityType == typeof(Setting)));
        return records;
    }

    //a key other than the id may hold a number, stored as one
    private static BsonValue Match(string value, string key)
    {
        return key != null && int.TryParse(value, out var number) ? number : value;
    }
}
