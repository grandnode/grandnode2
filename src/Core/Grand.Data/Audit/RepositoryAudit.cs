using Grand.Domain;
using System.Linq.Expressions;

namespace Grand.Data.Audit;

/// <summary>
///     The audit hooks shared by the Mongo and LiteDB auditing repositories
/// </summary>
public sealed class RepositoryAudit<T>(IAuditTracker tracker, Func<IReadOnlyCollection<string>, Task<IList<T>>> loadMany)
    where T : BaseEntity
{
    private bool Tracking => tracker.IsActive && tracker.IsAudited(typeof(T));

    public async Task Inserted(T entity)
    {
        if (!Tracking)
            return;

        await tracker.BeforeWriteAsync(entity.Id, true, loadMany);
        //the inserted entity is what was stored, so the flush does not read it back
        tracker.AfterFullUpdate(entity);
    }

    /// <summary>
    ///     A full update or a delete of one document
    /// </summary>
    public Task BeforeWrite(T entity)
    {
        return Tracking ? tracker.BeforeWriteAsync(entity.Id, false, loadMany) : Task.CompletedTask;
    }

    public void AfterFullUpdate(T entity)
    {
        if (Tracking)
            tracker.AfterFullUpdate(entity);
    }

    /// <summary>
    ///     A write of one field (or one collection) of one document; an empty id means every document
    /// </summary>
    public Task BeforeField(string id, LambdaExpression field, string operation)
    {
        if (!Tracking)
            return Task.CompletedTask;

        if (string.IsNullOrEmpty(id))
        {
            tracker.RecordBulk<T>(operation, field.ToString());
            return Task.CompletedTask;
        }

        return tracker.BeforeWriteAsync(id, false, loadMany, Paths([AuditFilterId.MemberPath(field)]));
    }

    public Task BeforeUpdateOne(Expression<Func<T, bool>> filter, UpdateBuilder<T> builder)
    {
        if (!Tracking)
            return Task.CompletedTask;

        if (!AuditFilterId.TryGetId(filter, out var id))
        {
            tracker.RecordBulk<T>("UpdateOneAsync", filter.ToString());
            return Task.CompletedTask;
        }

        return tracker.BeforeWriteAsync(id, false, loadMany, Paths(AuditFilterId.MemberPaths(builder)));
    }

    public void Bulk(string operation, string filter)
    {
        if (Tracking)
            tracker.RecordBulk<T>(operation, filter);
    }

    //an unreadable expression means "any field may have changed"
    private static IReadOnlyCollection<string> Paths(IList<string> paths)
    {
        return paths.Count == 0 || paths.Any(p => p == null) ? null : paths.ToList();
    }
}
