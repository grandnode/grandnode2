namespace Grand.Data;

/// <summary>
///     A lease lock with one document per held key, whose id is the key: inserting it takes the key, and the
///     database's unique id refuses a second holder. The expiry is kept as UTC ticks, never as a date, because LiteDB
///     reads dates back as local time.
/// </summary>
public abstract class ResourceLockBase(TimeSpan? lease = null, TimeSpan? wait = null) : IResourceLock
{
    protected const string CollectionName = "ResourceLock";
    protected const string OwnerField = "Owner";
    protected const string ExpiresUtcTicksField = "ExpiresUtcTicks";

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    private readonly TimeSpan _lease = lease ?? TimeSpan.FromSeconds(30);
    private readonly TimeSpan _wait = wait ?? TimeSpan.FromSeconds(10);

    public async Task<IAsyncDisposable> Acquire(IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var owner = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + _wait;
        var taken = new List<string>();
        try
        {
            //one order for every caller, so two callers wanting the same keys never wait on each other
            foreach (var key in keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
                while (true)
                {
                    //a holder whose process died never lets go
                    await DeleteExpired(key, DateTime.UtcNow.Ticks);
                    if (await TryInsert(key, owner, (DateTime.UtcNow + _lease).Ticks))
                    {
                        taken.Add(key);
                        break;
                    }

                    if (DateTime.UtcNow >= deadline)
                        throw new TimeoutException($"The resource '{key}' is in use by another operation");

                    await Task.Delay(RetryDelay, cancellationToken);
                }
        }
        catch
        {
            await Release(taken, owner);
            throw;
        }

        return new Handle(this, taken, owner);
    }

    /// <summary>
    ///     Inserts the key's document; false when another holder's document is there
    /// </summary>
    protected abstract Task<bool> TryInsert(string key, string owner, long expiresUtcTicks);

    /// <summary>
    ///     Deletes the key's document when its lease ended before <paramref name="nowUtcTicks" />
    /// </summary>
    protected abstract Task DeleteExpired(string key, long nowUtcTicks);

    /// <summary>
    ///     Deletes the key's document only when this owner still holds it: after a lease ran out the key may be held by
    ///     someone else
    /// </summary>
    protected abstract Task Delete(string key, string owner);

    private async Task Release(IEnumerable<string> keys, string owner)
    {
        foreach (var key in keys)
            await Delete(key, owner);
    }

    private sealed class Handle(ResourceLockBase resourceLock, IList<string> keys, string owner) : IAsyncDisposable
    {
        private int _disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                await resourceLock.Release(keys, owner);
        }
    }
}
