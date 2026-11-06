using MongoDB.Bson;
using MongoDB.Driver;

namespace Grand.Data.Mongo;

/// <summary>
///     Resolves the database only when a lock is taken, so it can be registered before installation
/// </summary>
public class MongoResourceLock(Func<IMongoDatabase> database, TimeSpan? lease = null, TimeSpan? wait = null)
    : ResourceLockBase(lease, wait)
{
    private static readonly FilterDefinitionBuilder<BsonDocument> Filter = Builders<BsonDocument>.Filter;

    protected override async Task<bool> TryInsert(string key, string owner, long expiresUtcTicks)
    {
        try
        {
            await Collection().InsertOneAsync(new BsonDocument {
                { "_id", key },
                { OwnerField, owner },
                { ExpiresUtcTicksField, expiresUtcTicks }
            });
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    protected override Task DeleteExpired(string key, long nowUtcTicks)
    {
        return Collection().DeleteOneAsync(Filter.Eq("_id", key) & Filter.Lt(ExpiresUtcTicksField, nowUtcTicks));
    }

    protected override Task Delete(string key, string owner)
    {
        return Collection().DeleteOneAsync(Filter.Eq("_id", key) & Filter.Eq(OwnerField, owner));
    }

    private IMongoCollection<BsonDocument> Collection()
    {
        return database().GetCollection<BsonDocument>(CollectionName);
    }
}
