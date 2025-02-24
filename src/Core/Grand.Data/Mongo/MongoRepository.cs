using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
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
        entity.UpdatedBy = _auditInfoProvider?.GetCurrentUser();
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
    /// <param name="filterExpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual async Task UpdateOneAsync(Expression<Func<T, bool>> filterExpression,
        UpdateBuilder<T> updateBuilder)
    {
        updateBuilder.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        updateBuilder.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var update = Builders<T>.Update.Combine(updateBuilder.Fields);
        await Collection.UpdateOneAsync(filterExpression, update);
    }

    /// <summary>
    ///     Updates a many entities
    /// </summary>
    /// <param name="filterExpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual async Task UpdateManyAsync(Expression<Func<T, bool>> filterExpression,
        UpdateBuilder<T> updateBuilder)
    {
        updateBuilder.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        updateBuilder.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var update = Builders<T>.Update.Combine(updateBuilder.Fields);
        await Collection.UpdateManyAsync(filterExpression, update);
    }

    /// <summary>
    ///     Add to set - add subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual async Task AddToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value)
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
    /// <typeparam name="Z">Subdocuments</typeparam>
    /// <param name="id">Ident of entitie</param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch">Subdocument field to match</param>
    /// <param name="elemMatch">Subdocument ident value</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    public virtual async Task UpdateToSet<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, Z>> elemFieldMatch, Z elemMatch, U value)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id)
                     & Builders<T>.Filter.ElemMatch(field, Builders<U>.Filter.Eq(elemFieldMatch, elemMatch));

        var me = (MemberExpression)field.Body;
        var minfo = me.Member;
        var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);
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
    /// <param name="elemFieldMatch">Subdocument field to match</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    public virtual async Task UpdateToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
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
    ///     Update subdocuments
    /// </summary>
    /// <typeparam name="T">Document</typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch">Subdocument field to match</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    /// <returns></returns>
    public virtual async Task UpdateToSet<U>(Expression<Func<T, IEnumerable<U>>> field, U elemFieldMatch, U value)
    {
        var me = (MemberExpression)field.Body;
        var minfo = me.Member;

        var filter = new BsonDocument {
            new BsonElement(minfo.Name, elemFieldMatch.ToString())
        };

        var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

        await Collection.UpdateManyAsync(filter, combinedUpdate);
    }

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="Z"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch"></param>
    /// <param name="elemMatch"></param>
    /// <returns></returns>
    public virtual async Task PullFilter<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, Z>> elemFieldMatch, Z elemMatch)
    {
        var filter = string.IsNullOrEmpty(id)
            ? Builders<T>.Filter.Where(x => true)
            : Builders<T>.Filter.Eq(x => x.Id, id);
        var update = Builders<T>.Update.PullFilter(field, Builders<U>.Filter.Eq(elemFieldMatch, elemMatch));

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
    public virtual async Task PullFilter<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        var update = Builders<T>.Update.PullFilter(field, elemFieldMatch);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

        await Collection.UpdateOneAsync(filter, combinedUpdate);
    }

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    public virtual async Task Pull(string id, Expression<Func<T, IEnumerable<string>>> field, string element)
    {
        var update = Builders<T>.Update.Pull(field, element);

        var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
        var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
        var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

        if (string.IsNullOrEmpty(id))
            await Collection.UpdateManyAsync(Builders<T>.Filter.Where(x => true), combinedUpdate);
        else
            await Collection.UpdateOneAsync(Builders<T>.Filter.Eq(x => x.Id, id), combinedUpdate);
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
}