using Grand.Domain;

namespace Grand.Data.Audit;

/// <summary>
///     Collects the staff writes of one request and turns them into audit entries.
///     Inactive (a pass-through) until the web layer activates it for a staff request.
/// </summary>
public interface IAuditTracker
{
    bool IsActive { get; }

    void Activate(AuditActor actor);

    bool IsAudited(Type entityType);

    /// <summary>
    ///     Called before a write of a single document (after it, for an insert). The first call for an id
    ///     snapshots the stored document with <paramref name="loadMany" />, which also reads the "after" state at the
    ///     flush; <paramref name="writtenPaths" /> null means every field.
    /// </summary>
    Task BeforeWriteAsync<T>(string id, bool isInsert, Func<IReadOnlyCollection<string>, Task<IList<T>>> loadMany,
        IReadOnlyCollection<string> writtenPaths = null) where T : BaseEntity;

    /// <summary>
    ///     Keeps the entity of a full update as its "after" state, so a later change by someone else before the flush
    ///     is not attributed to this request
    /// </summary>
    void AfterFullUpdate<T>(T entity) where T : BaseEntity;

    void RecordBulk<T>(string operation, string filter) where T : BaseEntity;

    /// <summary>
    ///     Records the inserts of <typeparamref name="T" /> made until the returned scope ends as one bulk entry with
    ///     their count (an import of thousands of records) instead of one entry each; updates and deletes stay single
    ///     entries. With <paramref name="owner" />, the entry shows in that record's change history.
    /// </summary>
    IDisposable CountInserts<T>(string operation, string details, BaseEntity owner = null) where T : BaseEntity;

    Task FlushAsync();

    /// <summary>
    ///     Flushes and stops tracking at the end of the request scope; later writes are not audited
    /// </summary>
    Task CompleteAsync();
}
