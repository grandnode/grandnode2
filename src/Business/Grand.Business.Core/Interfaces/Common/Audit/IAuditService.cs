using Grand.Domain;
using Grand.Domain.Audit;

namespace Grand.Business.Core.Interfaces.Common.Audit;

/// <summary>
///     Reads the staff audit trail and records events. Entries cannot be updated or deleted one by one;
///     only the retention task removes old ones.
/// </summary>
public interface IAuditService
{
    /// <summary>
    ///     Gets entries matching the search, newest first
    /// </summary>
    Task<IPagedList<AuditEntry>> GetEntries(AuditEntrySearch search, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<AuditEntry> GetById(string id);

    /// <summary>
    ///     Yields every entry matching the search, newest first, a page at a time (for exports)
    /// </summary>
    IAsyncEnumerable<AuditEntry> StreamEntries(AuditEntrySearch search, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the distinct entity types present in the log, sorted
    /// </summary>
    Task<IList<string>> GetEntityTypes();

    /// <summary>
    ///     Records an event that is not a data change (a sign-in, an export, an impersonation)
    /// </summary>
    Task InsertEvent(AuditEntry entry);

    /// <summary>
    ///     Deletes entries created before the given time; returns how many were deleted
    /// </summary>
    Task<int> DeleteOlderThan(DateTime createdBeforeUtc);
}
