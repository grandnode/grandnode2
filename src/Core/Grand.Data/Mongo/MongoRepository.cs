using Grand.Domain;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Grand.Data.Mongo;

/// <summary>
///     MongoDB repository
/// </summary>
public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    #region Fields

    private readonly IAuditInfoProvider _auditInfoProvider;

    /// <summary>
    ///     Gets the collection
    /// </summary>
    public IMongoCollection<T> Collection { get; protected init; }

    /// <summary>
    ///     Mongo Database
    /// </summary>
    protected IMongoDatabase Database { get; init; }

    #endregion

    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public MongoRepository(IAuditInfoProvider auditInfoProvider) : this(
        DataSettingsManager.Instance.LoadSettings().ConnectionString, auditInfoProvider)
    {
    }

    public MongoRepository(string connectionString, IAuditInfoProvider auditInfoProvider)
    {
        _auditInfoProvider = auditInfoProvider;

        if (!string.IsNullOrEmpty(connectionString))
        {
            var client = new MongoClient(connectionString);
            var databaseName = new MongoUrl(connectionString).DatabaseName;
            Database = client.GetDatabase(databaseName);
            Collection = Database.GetCollection<T>(typeof(T).Name);
        }
    }

    public MongoRepository(IMongoDatabase database, IAuditInfoProvider auditInfoProvider)
    {
        Database = database;
        _auditInfoProvider = auditInfoProvider;
        Collection = Database.GetCollection<T>(typeof(T).Name);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Get entity by identifier
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns>Entity</returns>
    public virtual T GetById(string id)
    {
        return Collection.Find(e => e.Id == id).FirstOrDefault();
    }

    /// <summary>
    ///     Get async entity by identifier
    /// </summary>
    /// <param name="id">Identifier</param>
    /// <returns>Entity</returns>
    public virtual Task<T> GetByIdAsync(string id)
    {
        return Collection.Find(e => e.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     Get async entity by expression
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns>Entity</returns>
    public virtual Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
    {
        return Collection.Find(predicate).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual T Insert(T entity)
    {
        entity.CreatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.CreatedBy = _auditInfoProvider.GetCurrentUser();
        Collection.InsertOne(entity);
        return entity;
    }

    /// <summary>
    ///     Async Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> InsertAsync(T entity)
    {
        entity.CreatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.CreatedBy = _auditInfoProvider.GetCurrentUser();
        await Collection.InsertOneAsync(entity);
        return entity;
    }

    /// <summary>
    ///     Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual T Update(T entity)
    {
        entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.UpdatedBy = _auditInfoProvider.GetCurrentUser();
        Collection.ReplaceOne(x => x.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = false });
        return entity;
    }

    /// <summary>
    ///     Async Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.UpdatedBy = _auditInfoProvider.GetCurrentUser();
        await Collection.ReplaceOneAsync(x => x.Id == entity.Id, entity,
            new ReplaceOptions { IsUpsert = false });
        return entity;
    }

    /// <summary>
    ///     Update field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    public virtual async Task UpdateField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        var builder = Builders<T>.Filter;
        var filter = builder.Eq(x => x.Id, id);
        var update = Builders<T>.Update
            .Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime())
            .Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser())
            .Set(expression, value);

        await Collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    ///     Inc field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    public virtual async Task IncField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        var builder = Builders<T>.Filter;
        var filter = builder.Eq(x => x.Id, id);
        var update = Builders<T>.Update
            .Inc(expression, value);

        await Collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    ///     Updates a single entity.
    /// </summary>
    /// <param name="filterexpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual async Task UpdateOneAsync(Expression<Func<T, bool>> filterexpression,
        UpdateBuilder<T> updateBuilder)
    {
        updateBuilder.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        updateBuilder.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var update = Builders<T>.Update.Combine(updateBuilder.Fields);
        await Collection.UpdateOneAsync(filterexpression, update);
    }

    /// <summary>
    ///     Updates a many entities
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual async Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression,
        UpdateBuilder<T> updateBuilder)
    {
        updateBuilder.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        updateBuilder.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var update = Builders<T>.Update.Combine(updateBuilder.Fields);
        await Collection.UpdateManyAsync(filterexpression, update);
    }

    /// <summary>
    ///     Add to set - add subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual async Task AddToCollectionField<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value)
    {
        var builder = Builders<T>.Filter;
        var filter = builder.Eq(x => x.Id, id);
        var update = Builders<T>.Update.AddToSet(field, value);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);
        await Collection.UpdateOneAsync(filter, combinedUpdate);
    }

    /// <summary>
    ///     Update subdocument
    /// </summary>
    /// <typeparam name="U">Document</typeparam>
    /// <param name="id">Ident of entitie</param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch">Subdocument predicate to match</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    public virtual async Task UpdateCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch, U value)
    {
        var filter = string.IsNullOrEmpty(id)
            ? Builders<T>.Filter.Where(x => true)
            : Builders<T>.Filter.Eq(x => x.Id, id)
              & Builders<T>.Filter.ElemMatch(field, elemFieldMatch);

        var me = (MemberExpression)field.Body;
        var minfo = me.Member;
        var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);
        if (string.IsNullOrEmpty(id))
            await Collection.UpdateManyAsync(filter, combinedUpdate);
        else
            await Collection.UpdateOneAsync(filter, combinedUpdate);
    }

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch"></param>
    /// <returns></returns>
    public virtual async Task RemoveCollectionFieldItem<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch)
    {
        var filter = string.IsNullOrEmpty(id)
            ? Builders<T>.Filter.Where(x => true)
            : Builders<T>.Filter.Eq(x => x.Id, id);
        var update = Builders<T>.Update.PullFilter(field, elemFieldMatch);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

        if (string.IsNullOrEmpty(id))
            await Collection.UpdateManyAsync(filter, combinedUpdate);
        else
            await Collection.UpdateOneAsync(filter, combinedUpdate);
    }

    /// <summary>
    ///     Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual void Delete(T entity)
    {
        Collection.FindOneAndDelete(e => e.Id == entity.Id);
    }

    /// <summary>
    ///     Async Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> DeleteAsync(T entity)
    {
        await Collection.DeleteOneAsync(e => e.Id == entity.Id);
        return entity;
    }

    /// <summary>
    ///     Async Delete entities
    /// </summary>
    /// <param name="entities">Entities</param>
    public virtual async Task DeleteAsync(IEnumerable<T> entities)
    {
        foreach (var entity in entities) await DeleteAsync(entity);
    }

    /// <summary>
    ///     Delete a many entities
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <returns></returns>
    public virtual async Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression)
    {
        await Collection.DeleteManyAsync(filterExpression);
    }

    /// <summary>
    ///     Clear entities
    /// </summary>
    public Task ClearAsync()
    {
        return Collection.DeleteManyAsync(Builders<T>.Filter.Empty);
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets a table
    /// </summary>
    public virtual IQueryable<T> Table => Collection.AsQueryable();

    /// <summary>
    ///     Gets a table collection
    /// </summary>
    public virtual IQueryable<C> TableCollection<C>() where C : class
    {
        return Database.GetCollection<C>(typeof(T).Name).AsQueryable();
    }

    #endregion

    #region Query execution

    /// <summary>
    ///     Executes the query and returns its results
    /// </summary>
    public virtual async Task<IList<TResult>> ToListAsync<TResult>(IQueryable<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     Executes the query and returns the number of matching documents
    /// </summary>
    public virtual async Task<int> CountAsync<TResult>(IQueryable<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    ///     Executes the query and returns a single page of its results
    /// </summary>
    public virtual async Task<IPagedList<TResult>> PagedAsync<TResult>(IQueryable<TResult> query, int pageIndex,
        int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        //keep the same normalization the paged list applies, so the skip matches the reported page size
        if (pageSize <= 0)
            pageSize = 1;

        var totalCount = await CountAsync(query, cancellationToken);
        var items = await ToListAsync(query.Skip(pageIndex * pageSize).Take(pageSize), cancellationToken);

        return new PagedList<TResult>(items, pageIndex, pageSize, totalCount);
    }

    #endregion
}