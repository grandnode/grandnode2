namespace Grand.Data;

/// <summary>
///     Serializes short critical sections across requests and application instances: a key is held by one caller at
///     a time, until the caller lets go or its lease runs out
/// </summary>
public interface IResourceLock
{
    /// <summary>
    ///     Waits until every key is free and holds them until the returned handle is disposed
    /// </summary>
    /// <exception cref="TimeoutException">A key stayed held by another caller for the whole wait</exception>
    Task<IAsyncDisposable> Acquire(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
