using Grand.Domain;
using Grand.SharedKernel.Attributes;
using LiteDB;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Data.LiteDb;

/// <summary>
///     MongoDB repository
/// </summary>
public class LiteDBRepository<T> : IRepository<T> where T : BaseEntity
{
    #region Fields

    private readonly IAuditInfoProvider _auditInfoProvider;

    /// <summary>
    ///     Gets the collection
    /// </summary>
    protected ILiteCollection<T> Collection { get; init; }
    
    /// <summary>
    ///     Mongo Database
    /// </summary>
    protected LiteDatabase Database { get; init; }
    
    #endregion

    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public LiteDBRepository(IAuditInfoProvider auditInfoProvider)
    {
        _auditInfoProvider = auditInfoProvider;
        var connection = DataSettingsManager.Instance.LoadSettings();

        if (!string.IsNullOrEmpty(connection.ConnectionString))
        {
            Database = new LiteDatabase(connection.ConnectionString);
            Collection = Database.GetCollection<T>(typeof(T).Name);
        }
    }

    public LiteDBRepository(string connectionString, IAuditInfoProvider auditInfoProvider)
    {
        _auditInfoProvider = auditInfoProvider;
        Database = new LiteDatabase(connectionString);
        Collection = Database.GetCollection<T>(typeof(T).Name);
    }


    public LiteDBRepository(LiteDatabase database, IAuditInfoProvider auditInfoProvider)
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
    public virtual async Task<T> GetByIdAsync(string id)
    {
        return await Task.FromResult(GetById(id));
    }

    /// <summary>
    ///     Get async entity by expression
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns>Entity</returns>
    public virtual Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
    {
        var visitor = new ToLowerInvariantVisitor();
        //WORKAROUND Issue #479 - Method ToLowerInvariant() in String are not supported when convert to BsonExpression
        var modifiedExpression = (Expression<Func<T, bool>>)visitor.Visit(predicate);
        return Task.FromResult(Collection.Find(modifiedExpression).FirstOrDefault());
    }

    /// <summary>
    ///     Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual T Insert(T entity)
    {
        entity.CreatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.CreatedBy = _auditInfoProvider.GetCurrentUser();
        Collection.Insert(entity);
        return entity;
    }

    /// <summary>
    ///     Async Insert entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> InsertAsync(T entity)
    {
        Insert(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    ///     Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual T Update(T entity)
    {
        entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.UpdatedBy = _auditInfoProvider.GetCurrentUser();
        Collection.Update(entity);
        return entity;
    }

    /// <summary>
    ///     Async Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        Update(entity);
        return await Task.FromResult(entity);
    }

    /// <summary>
    ///     Update field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    public virtual Task UpdateField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        var entity = Database.GetCollection(typeof(T).Name).FindById(new BsonValue(id));
        var bsonValue = BsonMapper.Global.Serialize(value);
        entity[GetName(expression)] = bsonValue;
        entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
        entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
        Database.GetCollection(typeof(T).Name).Update(entity);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Inc field for entity
    /// </summary>
    /// <typeparam name="U">Value</typeparam>
    /// <param name="id">Ident record</param>
    /// <param name="expression">Linq Expression</param>
    /// <param name="value">value</param>
    public virtual Task IncField<U>(string id, Expression<Func<T, U>> expression, U value)
    {
        var entity = Database.GetCollection(typeof(T).Name).FindById(new BsonValue(id));
        switch (value)
        {
            case int intValue:
                var intrawValue = Convert.ToInt32(entity[GetName(expression)].RawValue);
                var bsonIntValue = BsonMapper.Global.Serialize(intrawValue + intValue);
                entity[GetName(expression)] = bsonIntValue;
                Database.GetCollection(typeof(T).Name).Update(entity);
                break;
            case long longValue:
                var longrawValue = Convert.ToInt64(entity[GetName(expression)].RawValue);
                var bsonLongValue = BsonMapper.Global.Serialize(longrawValue + longValue);
                entity[GetName(expression)] = bsonLongValue;
                Database.GetCollection(typeof(T).Name).Update(entity);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Updates a single entity.
    /// </summary>
    /// <param name="filterexpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual Task UpdateOneAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder)
    {
        var entity = Collection.FindOne(filterexpression);
        Update(entity, updateBuilder);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Updates a many entities
    /// </summary>
    /// <param name="filterexpression"></param>
    /// <param name="updateBuilder"></param>
    /// <returns></returns>
    public virtual Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder)
    {
        var entities = Collection.Find(filterexpression);
        foreach (var entity in entities) Update(entity, updateBuilder);
        return Task.CompletedTask;
    }

    private Task Update(T entity, UpdateBuilder<T> updateBuilder)
    {
        foreach (var item in updateBuilder.ExpressionFields)
        {
            var name = GetName(item.Expression);

            var propertyInfo = entity?.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance);

            propertyInfo?.SetValue(entity, item.Value);
        }

        entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
        entity.UpdatedBy = _auditInfoProvider.GetCurrentUser();

        Collection.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Add to set - add subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Task AddToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value)
    {
        var collection = Database.GetCollection(Collection.Name);
        var entity = collection.FindById(new BsonValue(id));
        var fieldName = ((MemberExpression)field.Body).Member.Name;

        if (entity[fieldName].IsArray)
        {
            var bsonValue = BsonMapper.Global.Serialize(value);
            var list = entity[fieldName].AsArray.ToList();
            list.Add(bsonValue);
            entity[fieldName] = new BsonArray(list);
            entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
            entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
            collection.Update(entity);
        }

        return Task.CompletedTask;
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
    public virtual Task UpdateToSet<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, Z>> elemFieldMatch, Z elemMatch, U value)
    {
        var collection = Database.GetCollection(Collection.Name);
        var entity = collection.FindById(new BsonValue(id));
        var fieldName = ((MemberExpression)field.Body).Member.Name;

        var member = ((MemberExpression)elemFieldMatch.Body).Member;
        var dBFieldName = member.GetCustomAttribute<DBFieldNameAttribute>();
        var elementfieldName = dBFieldName?.Name ?? member.Name;

        if (entity[fieldName].IsArray)
        {
            var bsonValue = BsonMapper.Global.Serialize(value);
            var list = entity[fieldName].AsArray.ToList();
            var document = list.FirstOrDefault(x => x[elementfieldName] == new BsonValue(elemMatch));
            if (document == null) return Task.CompletedTask;

            foreach (var key in bsonValue.AsDocument.Keys) document[key] = bsonValue[key];
            entity[fieldName] = new BsonArray(list);
            entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
            entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
            collection.Update(entity);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Update subdocument
    /// </summary>
    /// <typeparam name="U">Document</typeparam>
    /// <param name="id">Ident of entitie</param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch">Subdocument field to match</param>
    /// <param name="value">Subdocument - to update (all values)</param>
    public virtual Task UpdateToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch, U value)
    {
        var collection = Database.GetCollection(Collection.Name);
        var entity = collection.FindById(new BsonValue(id));
        var fieldName = ((MemberExpression)field.Body).Member.Name;
        if (entity == null) return Task.CompletedTask;

        if (entity[fieldName].IsArray)
        {
            var bsonValue = BsonMapper.Global.Serialize(value);
            var list = BsonMapper.Global.Deserialize<IList<U>>(entity[fieldName]).ToList();
            var position = list.FirstOrDefault(elemFieldMatch.Compile());
            if (position == null) return Task.CompletedTask;

            foreach (var item in position.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyField = position.GetType().GetProperty(item.Name,
                    BindingFlags.Public | BindingFlags.Instance);

                propertyField?.SetValue(position, item.GetValue(value));
            }

            var updateList = BsonMapper.Global.Serialize<IList<U>>(list);
            entity[fieldName] = updateList;
            entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
            entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();

            collection.Update(entity);
        }

        return Task.CompletedTask;
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
    public virtual Task UpdateToSet<U>(Expression<Func<T, IEnumerable<U>>> field, U elemFieldMatch, U value)
    {
        var collection = Database.GetCollection(Collection.Name);
        var fieldName = ((MemberExpression)field.Body).Member.Name;
        var records = collection.Find(Query.EQ($"{fieldName}[*] ANY", elemFieldMatch.ToString())).ToList();
        foreach (var entity in records)
            if (entity[fieldName].IsArray)
            {
                var bsonValue = BsonMapper.Global.Serialize(value);
                var oldbsonValue = BsonMapper.Global.Serialize(elemFieldMatch);
                var list = entity[fieldName].AsArray.ToList();
                if (list != null && list.Any())
                {
                    list.Add(bsonValue);
                    list.Remove(oldbsonValue);
                    entity[fieldName] = new BsonArray(list);
                    entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
                    entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
                    collection.Update(entity);
                }
            }

        return Task.CompletedTask;
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
    public virtual Task PullFilter<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, Z>> elemFieldMatch, Z elemMatch)
    {
        var collection = Database.GetCollection(Collection.Name);
        var fieldName = ((MemberExpression)field.Body).Member.Name;

        var member = ((MemberExpression)elemFieldMatch.Body).Member;
        var dBFieldName = member.GetCustomAttribute<DBFieldNameAttribute>();
        var elementfieldName = dBFieldName?.Name ?? member.Name;

        if (string.IsNullOrEmpty(id))
        {
            //update many
            var entities = collection.FindAll();
            foreach (var entity in entities) UpdateEntity(entity);
        }
        else
        {
            //update one
            var entity = collection.FindById(new BsonValue(id));
            UpdateEntity(entity);
        }

        void UpdateEntity(BsonDocument entity)
        {
            if (entity != null && entity[fieldName].IsArray)
            {
                var bsonValue = BsonMapper.Global.Serialize(elemMatch);
                var list = entity[fieldName].AsArray.ToList();
                var documents = list.Where(x => x[elementfieldName] == new BsonValue(elemMatch)).ToList();
                if (documents != null && documents.Any())
                {
                    foreach (var document in documents) list.Remove(document);
                    entity[fieldName] = new BsonArray(list);
                    entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
                    entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
                    collection.Update(entity);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="elemFieldMatch"></param>
    /// <returns></returns>
    public virtual Task PullFilter<U>(string id, Expression<Func<T, IEnumerable<U>>> field,
        Expression<Func<U, bool>> elemFieldMatch)
    {
        var collection = Database.GetCollection(Collection.Name);
        var entity = collection.FindById(new BsonValue(id));
        var fieldName = ((MemberExpression)field.Body).Member.Name;
        if (entity == null) return Task.CompletedTask;

        if (entity[fieldName].IsArray)
        {
            var list = BsonMapper.Global.Deserialize<IList<U>>(entity[fieldName]).ToList();
            var position = list.FirstOrDefault(elemFieldMatch.Compile());
            if (position == null) return Task.CompletedTask;

            list.Remove(position);

            var updatelist = BsonMapper.Global.Serialize<IList<U>>(list);
            entity[fieldName] = updatelist;
            entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
            entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
            collection.Update(entity);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Delete subdocument
    /// </summary>
    /// <param name="id"></param>
    /// <param name="field"></param>
    /// <param name="element"></param>
    /// <returns></returns>
    public virtual Task Pull(string id, Expression<Func<T, IEnumerable<string>>> field, string element)
    {
        var collection = Database.GetCollection(Collection.Name);
        var fieldName = ((MemberExpression)field.Body).Member.Name;
        if (string.IsNullOrEmpty(id))
        {
            var entities = collection.Find(Query.EQ($"{fieldName}[*] ANY", element)).ToList();
            foreach (var entity in entities) UpdateEntity(entity);
        }
        else
        {
            //update one
            var entity = collection.FindById(new BsonValue(id));
            UpdateEntity(entity);
        }

        void UpdateEntity(BsonDocument entity)
        {
            if (entity != null && entity[fieldName].IsArray)
            {
                var list = entity[fieldName].AsArray.ToList();
                if (list != null && list.Any())
                {
                    list.Remove(new BsonValue(element));
                    entity[fieldName] = new BsonArray(list);
                    entity["UpdatedOnUtc"] = _auditInfoProvider.GetCurrentDateTime();
                    entity["UpdatedBy"] = _auditInfoProvider.GetCurrentUser();
                    collection.Update(entity);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual void Delete(T entity)
    {
        Collection.Delete(new BsonValue(entity.Id));
    }

    /// <summary>
    ///     Async Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    public virtual async Task<T> DeleteAsync(T entity)
    {
        Delete(entity);
        return await Task.FromResult(entity);
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
    public virtual Task DeleteManyAsync(Expression<Func<T, bool>> filterExpression)
    {
        Collection.DeleteMany(filterExpression);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Clear entities
    /// </summary>
    public Task ClearAsync()
    {
        Collection.DeleteAll();
        return Task.CompletedTask;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets a table
    /// </summary>
    public virtual IQueryable<T> Table => Collection.Query().ToEnumerable().AsQueryable();

    /// <summary>
    ///     Gets a table collection
    /// </summary>
    public virtual IQueryable<C> TableCollection<C>() where C : class
    {
        return Database.GetCollection<C>(nameof(T)).Query().ToEnumerable().AsQueryable();
    }

    #endregion

    #region Helpers

    private string GetName(LambdaExpression lambdaexpression)
    {
        var expression = (MemberExpression)lambdaexpression.Body;
        return expression.Member.Name;
    }

    private string GetName<TSource, TField>(Expression<Func<TSource, TField>> Field)
    {
        if (Equals(Field, null)) throw new NullReferenceException("Field is required");

        MemberExpression expr = null;

        switch (Field.Body)
        {
            case MemberExpression expression:
                expr = expression;
                break;
            case UnaryExpression expression:
                expr = (MemberExpression)expression.Operand;
                break;
            default:
            {
                const string format = "Expression '{0}' not supported.";
                var message = string.Format(format, Field);

                throw new ArgumentException(message, nameof(Field));
            }
        }

        return expr.Member.Name;
    }

    #endregion
}