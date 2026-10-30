using Grand.Data.Audit;
using LiteDB;

namespace Grand.Data.LiteDb;

/// <summary>
///     Resolves the database only when a record is read, so it can be registered before installation
/// </summary>
public class LiteDBAuditRecordReader(Func<LiteDatabase> database) : IAuditRecordReader
{
    //the embedded database reads the whole document either way, so the field is not projected
    public Task<IDictionary<string, string>> Read(Type entityType, string value, string key = null,
        string field = null)
    {
        var db = database();
        var collection = db.GetCollection(entityType.Name);
        //a key other than the id may hold a number, stored as one
        var document = key == null
            ? collection.FindById(new BsonValue(value))
            : collection.FindOne(Query.EQ(key, int.TryParse(value, out var number) ? new BsonValue(number) : new BsonValue(value)));
        //through the entity, so the snapshot has the same shape as on MongoDB
        var entity = document == null ? null : db.Mapper.ToObject(entityType, document);
        return Task.FromResult(entity == null ? null : AuditSnapshot.Take(entity, entityType));
    }
}
