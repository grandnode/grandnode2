using LiteDB;

namespace Grand.Data.LiteDb;

/// <summary>
///     Resolves the database only when a lock is taken, so it can be registered before installation
/// </summary>
public class LiteDBResourceLock(Func<LiteDatabase> database, TimeSpan? lease = null, TimeSpan? wait = null)
    : ResourceLockBase(lease, wait)
{
    protected override Task<bool> TryInsert(string key, string owner, long expiresUtcTicks)
    {
        try
        {
            Collection().Insert(new BsonDocument {
                ["_id"] = key,
                [OwnerField] = owner,
                [ExpiresUtcTicksField] = expiresUtcTicks
            });
            return Task.FromResult(true);
        }
        catch (LiteException ex) when (ex.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
        {
            return Task.FromResult(false);
        }
    }

    protected override Task DeleteExpired(string key, long nowUtcTicks)
    {
        Collection().DeleteMany(Query.And(Query.EQ("_id", key), Query.LT(ExpiresUtcTicksField, nowUtcTicks)));
        return Task.CompletedTask;
    }

    protected override Task Delete(string key, string owner)
    {
        Collection().DeleteMany(Query.And(Query.EQ("_id", key), Query.EQ(OwnerField, owner)));
        return Task.CompletedTask;
    }

    private ILiteCollection<BsonDocument> Collection()
    {
        return database().GetCollection(CollectionName);
    }
}
