using Grand.Data.Audit;
using Grand.Domain;
using LiteDB;
using System.Linq.Expressions;

namespace Grand.Data.LiteDb;

/// <summary>
///     LiteDB repository that reports staff writes to the audit tracker
/// </summary>
public class AuditingLiteDBRepository<T> : LiteDBRepository<T> where T : BaseEntity
{
    private readonly RepositoryAudit<T> _audit;

    public AuditingLiteDBRepository(LiteDatabase database, IAuditInfoProvider auditInfoProvider,
        IAuditTracker tracker) : base(database, auditInfoProvider)
    {
        _audit = new RepositoryAudit<T>(tracker, new AuditRepositoryAccess<T>(LoadMany));
    }

    //used before installation, when no database is registered yet
    public AuditingLiteDBRepository(IAuditInfoProvider auditInfoProvider, IAuditTracker tracker)
        : base(auditInfoProvider)
    {
        _audit = new RepositoryAudit<T>(tracker, new AuditRepositoryAccess<T>(LoadMany));
    }

    //by id, one at a time: Table would read the whole collection
    private Task<IList<T>> LoadMany(IReadOnlyCollection<string> ids)
    {
        IList<T> found = ids.Select(id => Collection.FindById(new BsonValue(id))).Where(e => e != null).ToList();
        return Task.FromResult(found);
    }

    public override async Task<T> InsertAsync(T entity)
    {
        var inserted = await base.InsertAsync(entity);
        await _audit.Inserted(entity);
        return inserted;
    }

    public override async Task<T> UpdateAsync(T entity)
    {
        await _audit.BeforeFullUpdate(entity);
        var updated = await base.UpdateAsync(entity);
        _audit.AfterFullUpdate(entity);
        return updated;
    }

    public override async Task UpdateField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        await _audit.BeforeField(id, expression, nameof(UpdateField));
        await base.UpdateField(id, expression, value);
    }

    public override async Task IncField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        await _audit.BeforeField(id, expression, nameof(IncField));
        await base.IncField(id, expression, value);
    }

    public override async Task UpdateOneAsync(Expression<Func<T, bool>> filterexpression,
        UpdateBuilder<T> updateBuilder)
    {
        await _audit.BeforeUpdateOne(filterexpression, updateBuilder);
        await base.UpdateOneAsync(filterexpression, updateBuilder);
    }

    public override async Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression,
        UpdateBuilder<T> updateBuilder)
    {
        _audit.Bulk(nameof(UpdateManyAsync), filterexpression.ToString());
        await base.UpdateManyAsync(filterexpression, updateBuilder);
    }

    public override async Task AddToCollectionField<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value)
    {
        await _audit.BeforeField(id, field, nameof(AddToCollectionField));
        await base.AddToCollectionField(id, field, value);
    }

    public override async Task UpdateCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch, U value)
    {
        await _audit.BeforeField(id, field, nameof(UpdateCollectionFieldItem));
        await base.UpdateCollectionFieldItem(id, field, elemFieldMatch, value);
    }

    public override async Task RemoveCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch)
    {
        await _audit.BeforeField(id, field, nameof(RemoveCollectionFieldItem));
        await base.RemoveCollectionFieldItem(id, field, elemFieldMatch);
    }

    public override async Task<T> DeleteAsync(T entity)
    {
        await _audit.BeforeDelete(entity);
        return await base.DeleteAsync(entity);
    }

    public override async Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression)
    {
        _audit.Bulk(nameof(DeleteManyAsync), filterExpression.ToString());
        await base.DeleteManyAsync(filterExpression);
    }

    public override Task ClearAsync()
    {
        _audit.Bulk(nameof(ClearAsync), "all documents");
        return base.ClearAsync();
    }
}
