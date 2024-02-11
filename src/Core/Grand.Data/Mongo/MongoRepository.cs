using Grand.Domain;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Data.Mongo
{
    /// <summary>
    /// MongoDB repository
    /// </summary>
    public class MongoRepository<T> : IRepository<T> where T : BaseEntity
    {
        #region Fields

        private readonly IAuditInfoProvider _auditInfoProvider;

        /// <summary>
        /// Gets the collection
        /// </summary>
        protected IMongoCollection<T> _collection;

        public IMongoCollection<T> Collection => _collection;

        /// <summary>
        /// Sets a collection
        /// </summary>
        public bool SetCollection(string collectionName)
        {
            _collection = _collection.Database.GetCollection<T>(collectionName);
            return true;
        }

        /// <summary>
        /// Mongo Database
        /// </summary>
        protected IMongoDatabase _database;

        public IMongoDatabase Database => _database;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>        
        public MongoRepository(IAuditInfoProvider auditInfoProvider)
        {
            _auditInfoProvider = auditInfoProvider;
            var connection = DataSettingsManager.LoadSettings();

            if (!string.IsNullOrEmpty(connection.ConnectionString))
            {
                var client = new MongoClient(connection.ConnectionString);
                var databaseName = new MongoUrl(connection.ConnectionString).DatabaseName;
                _database = client.GetDatabase(databaseName);
                _collection = _database.GetCollection<T>(typeof(T).Name);
            }
        }

        public MongoRepository(string connectionString, IAuditInfoProvider auditInfoProvider)
        {
            _auditInfoProvider = auditInfoProvider;
            var client = new MongoClient(connectionString);
            var databaseName = new MongoUrl(connectionString).DatabaseName;
            _database = client.GetDatabase(databaseName);
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }


        public MongoRepository(IMongoDatabase database, IAuditInfoProvider auditInfoProvider)
        {
            _database = database;
            _auditInfoProvider = auditInfoProvider;
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get entity by identifier
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        public virtual T GetById(string id)
        {
            return _collection.Find(e => e.Id == id).FirstOrDefault();
        }

        /// <summary>
        /// Get async entity by identifier 
        /// </summary>
        /// <param name="id">Identifier</param>
        /// <returns>Entity</returns>
        public virtual Task<T> GetByIdAsync(string id)
        {
            return _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get async entity by expression 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>Entity</returns>
        public virtual Task<T> GetOneAsync(Expression<Func<T, bool>> predicate)
        {
            return _collection.Find(predicate).FirstOrDefaultAsync();
        }
        
        /// <summary>
        /// Insert entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual T Insert(T entity)
        {
            entity.CreatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
            entity.CreatedBy = _auditInfoProvider.GetCurrentUser();
            _collection.InsertOne(entity);
            return entity;
        }

        /// <summary>
        /// Async Insert entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> InsertAsync(T entity)
        {
            entity.CreatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
            entity.CreatedBy = _auditInfoProvider.GetCurrentUser();
            await _collection.InsertOneAsync(entity);
            return entity;
        }

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual T Update(T entity)
        {
            entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
            entity.UpdatedBy = _auditInfoProvider?.GetCurrentUser();
            _collection.ReplaceOne(x => x.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = false });
            return entity;
        }

        /// <summary>
        /// Async Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            entity.UpdatedOnUtc = _auditInfoProvider.GetCurrentDateTime();
            entity.UpdatedBy = _auditInfoProvider.GetCurrentUser();
            await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity,
                new ReplaceOptions { IsUpsert = false });
            return entity;
        }

        /// <summary>
        /// Update field for entity
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

            await _collection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Inc field for entity
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

            await _collection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Updates a single entity.
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
            await _collection.UpdateOneAsync(filterexpression, update);
        }

        /// <summary>
        /// Updates a many entities
        /// </summary>
        /// <param name="filterexpression"></param>
        /// <param name="updateBuilder"></param>
        /// <returns></returns>
        public virtual async Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression,
            UpdateBuilder<T> updateBuilder)
        {
            updateBuilder.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
            updateBuilder.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
            var update = Builders<T>.Update.Combine(updateBuilder.Fields);
            await _collection.UpdateManyAsync(filterexpression, update);
        }

        /// <summary>
        /// Add to set - add subdocument
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
            await _collection.UpdateOneAsync(filter, combinedUpdate);
        }

        /// <summary>
        /// Update subdocument
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

            MemberExpression me = field.Body as MemberExpression;
            MemberInfo minfo = me.Member;
            var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);
            var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
            var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
            var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

            await _collection.UpdateOneAsync(filter, combinedUpdate);
        }

        /// <summary>
        /// Update subdocument
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

            MemberExpression me = field.Body as MemberExpression;
            MemberInfo minfo = me.Member;
            var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

            var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
            var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
            var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);
            if (string.IsNullOrEmpty(id))
            {
                await _collection.UpdateManyAsync(filter, combinedUpdate);
            }
            else
            {
                await _collection.UpdateOneAsync(filter, combinedUpdate);
            }
        }

        /// <summary>
        /// Update subdocuments
        /// </summary>
        /// <typeparam name="T">Document</typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="field"></param>
        /// <param name="elemFieldMatch">Subdocument field to match</param>
        /// <param name="value">Subdocument - to update (all values)</param>
        /// <returns></returns>
        public virtual async Task UpdateToSet<U>(Expression<Func<T, IEnumerable<U>>> field, U elemFieldMatch, U value)
        {
            MemberExpression me = field.Body as MemberExpression;
            MemberInfo minfo = me.Member;

            var filter = new BsonDocument {
                new BsonElement(minfo.Name, elemFieldMatch.ToString())
            };

            var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

            var updateDate = Builders<T>.Update.Set(x => x.UpdatedOnUtc, _auditInfoProvider.GetCurrentDateTime());
            var updateUser = Builders<T>.Update.Set(x => x.UpdatedBy, _auditInfoProvider.GetCurrentUser());
            var combinedUpdate = Builders<T>.Update.Combine(update, updateDate, updateUser);

            await _collection.UpdateManyAsync(filter, combinedUpdate);
        }

        /// <summary>
        /// Delete subdocument
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
            {
                await _collection.UpdateManyAsync(filter, combinedUpdate);
            }
            else
            {
                await _collection.UpdateOneAsync(filter, combinedUpdate);
            }
        }

        /// <summary>
        /// Delete subdocument
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

            await _collection.UpdateOneAsync(filter, combinedUpdate);
        }

        /// <summary>
        /// Delete subdocument
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
            {
                await _collection.UpdateManyAsync(Builders<T>.Filter.Where(x => true), combinedUpdate);
            }
            else
            {
                await _collection.UpdateOneAsync(Builders<T>.Filter.Eq(x => x.Id, id), combinedUpdate);
            }
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Delete(T entity)
        {
            _collection.FindOneAndDelete(e => e.Id == entity.Id);
        }

        /// <summary>
        /// Async Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> DeleteAsync(T entity)
        {
            await _collection.DeleteOneAsync(e => e.Id == entity.Id);
            return entity;
        }

        /// <summary>
        /// Async Delete entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual async Task<IEnumerable<T>> DeleteAsync(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                await DeleteAsync(entity);
            }

            return entities;
        }

        /// <summary>
        /// Delete a many entities
        /// </summary>
        /// <param name="filterexpression"></param>
        /// <returns></returns>
        public virtual async Task DeleteManyAsync(Expression<Func<T, bool>> filterexpression)
        {
            await _collection.DeleteManyAsync(filterexpression);
        }

        /// <summary>
        /// Clear entities
        /// </summary>
        public Task ClearAsync()
        {
            return _collection.DeleteManyAsync(Builders<T>.Filter.Empty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a table
        /// </summary>
        public virtual IQueryable<T> Table => _collection.AsQueryable();

        /// <summary>
        /// Gets a table collection
        /// </summary>
        public virtual IQueryable<T> TableCollection(string collectionName)
        {
            return _collection.Database.GetCollection<T>(collectionName).AsQueryable();
        }

        #endregion
    }
}