using Grand.Data.Audit;
using LiteDB;

namespace Grand.Data.LiteDb;

/// <summary>
///     Resolves the database only when a record is read, so it can be registered before installation
/// </summary>
public class LiteDBAuditRecordReader(Func<LiteDatabase> database) : IAuditRecordReader
{
    public Task<IDictionary<string, string>> Read(Type entityType, string id)
    {
        var db = database();
        var document = db.GetCollection(entityType.Name).FindById(new BsonValue(id));
        //through the entity, so the snapshot has the same shape as on MongoDB
        var entity = document == null ? null : db.Mapper.ToObject(entityType, document);
        return Task.FromResult(entity == null ? null : AuditSnapshot.Take(entity, entityType));
    }
}
