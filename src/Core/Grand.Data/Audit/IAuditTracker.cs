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
    ///     snapshots the stored document; <paramref name="writtenPaths" /> null means every field.
    /// </summary>
    Task BeforeWriteAsync<T>(string id, bool isInsert, AuditRepositoryAccess<T> access,
        IReadOnlyCollection<string> writtenPaths = null) where T : BaseEntity;

    /// <summary>
    ///     Keeps the entity of a full update as its "after" state, so the flush does not re-read it
    /// </summary>
    void AfterFullUpdate<T>(T entity) where T : BaseEntity;

    void RecordBulk<T>(string operation, string filter) where T : BaseEntity;

    Task FlushAsync();
}
