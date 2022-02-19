using LiteDB;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Domain.Data.LiteDb
{
    /// <summary>
    /// MongoDB repository
    /// </summary>
    public partial class LiteDBRepository<T> : IRepository<T> where T : BaseEntity
    {
        #region Fields

        /// <summary>
        /// Gets the collection
        /// </summary>
        protected ILiteCollection<T> _collection;

        public ILiteCollection<T> Collection {
            get {
                return _collection;
            }
        }

        /// <summary>
        /// Sets a collection
        /// </summary>
        public bool SetCollection(string collectionName)
        {
            _collection = _database.GetCollection<T>(collectionName);
            return true;
        }

        /// <summary>
        /// Mongo Database
        /// </summary>
        protected LiteDatabase _database;
        public LiteDatabase Database {
            get {
                return _database;
            }
        }

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>        
        public LiteDBRepository()
        {
            var connection = DataSettingsManager.LoadSettings();

            if (!string.IsNullOrEmpty(connection.ConnectionString))
            {
                _database = new LiteDatabase(connection.ConnectionString);
                _collection = _database.GetCollection<T>(typeof(T).Name);
            }
        }
        public LiteDBRepository(string connectionString)
        {
            _database = new LiteDatabase(connectionString);
            _collection = _database.GetCollection<T>(typeof(T).Name);
        }


        public LiteDBRepository(LiteDatabase database)
        {
            _database = database;
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
        public virtual async Task<T> GetByIdAsync(string id)
        {
            return await Task.FromResult(GetById(id));
        }

        /// <summary>
        /// Get all entities in collection
        /// </summary>
        /// <returns>collection of entities</returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await Task.FromResult(_collection.Query().ToList());
        }

        /// <summary>
        /// get first item in query as async
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter)
        {
            return await Task.FromResult(_collection.Find(filter).FirstOrDefault());
        }

        /// <summary>
        /// Insert entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual T Insert(T entity)
        {
            _collection.Insert(entity);
            return entity;
        }

        /// <summary>
        /// Async Insert entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> InsertAsync(T entity)
        {
            Insert(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// Async Insert many entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual Task InsertManyAsync(IEnumerable<T> entities)
        {
            Insert(entities);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Insert entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Insert(IEnumerable<T> entities)
        {
            _collection.InsertBulk(entities);
        }

        /// <summary>
        /// Async Insert entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual async Task<IEnumerable<T>> InsertAsync(IEnumerable<T> entities)
        {
            Insert(entities);
            return await Task.FromResult(entities);
        }


        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual T Update(T entity)
        {
            _collection.Update(entity);
            return entity;

        }

        /// <summary>
        /// Async Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            Update(entity);
            return await Task.FromResult(entity);
        }


        /// <summary>
        /// Update entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Update(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                Update(entity);
            }
        }

        /// <summary>
        /// Update field for entity
        /// </summary>
        /// <typeparam name="U">Value</typeparam>
        /// <param name="id">Ident record</param>
        /// <param name="expression">Linq Expression</param>
        /// <param name="value">value</param>
        public virtual Task UpdateField<U>(string id, Expression<Func<T, U>> expression, U value)
        {

            //TODO
            /*
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var update = Builders<T>.Update
                .Set(expression, value);

            await _collection.UpdateOneAsync(filter, update);
            */
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates a single entity.
        /// </summary>
        /// <param name="filterexpression"></param>
        /// <param name="updateBuilder"></param>
        /// <returns></returns>
        public virtual Task UpdateOneAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder)
        {
            //TODO 
            //var update = Builders<T>.Update.Combine(updateBuilder.Fields);
            //await _collection.UpdateOneAsync(filterexpression, update);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates a many entities
        /// </summary>
        /// <param name="filterexpression"></param>
        /// <param name="updateBuilder"></param>
        /// <returns></returns>
        public virtual Task UpdateManyAsync(Expression<Func<T, bool>> filterexpression, UpdateBuilder<T> updateBuilder)
        {
            //TODO 
            //var update = Builders<T>.Update.Combine(updateBuilder.Fields);
            //await _collection.UpdateManyAsync(filterexpression, update);
            return Task.CompletedTask;
        }

        // <summary>
        /// Add to set - add subdocument
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="id"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Task AddToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field, U value)
        {
            //TODO 
            //var builder = Builders<T>.Filter;
            //var filter = builder.Eq(x => x.Id, id);
            //var update = Builders<T>.Update.AddToSet(field, value);

            //await _collection.UpdateOneAsync(filter, update);
            return Task.CompletedTask;

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
        public virtual Task UpdateToSet<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, Z>> elemFieldMatch, Z elemMatch, U value)
        {
            //TODO

            //var filter = Builders<T>.Filter.Eq(x => x.Id, id)
            //    & Builders<T>.Filter.ElemMatch(field, Builders<U>.Filter.Eq(elemFieldMatch, elemMatch));

            //MemberExpression me = field.Body as MemberExpression;
            //MemberInfo minfo = me.Member;
            //var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

            //await _collection.UpdateOneAsync(filter, update);
            return Task.CompletedTask;

        }

        /// <summary>
        /// Update subdocument
        /// </summary>
        /// <typeparam name="U">Document</typeparam>
        /// <param name="id">Ident of entitie</param>
        /// <param name="field"></param>
        /// <param name="elemFieldMatch">Subdocument field to match</param>
        /// <param name="elemMatch">Subdocument ident value</param>
        /// <param name="value">Subdocument - to update (all values)</param>
        public virtual Task UpdateToSet<U>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, bool>> elemFieldMatch, U value, bool updateMany = false)
        {
            //TODO
            //var filter = string.IsNullOrEmpty(id) ? Builders<T>.Filter.Where(x => true) : Builders<T>.Filter.Eq(x => x.Id, id)
            //    & Builders<T>.Filter.ElemMatch(field, elemFieldMatch);

            //MemberExpression me = field.Body as MemberExpression;
            //MemberInfo minfo = me.Member;
            //var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);

            //if (updateMany)
            //{
            //    await _collection.UpdateManyAsync(filter, update);
            //}
            //else
            //{
            //    await _collection.UpdateOneAsync(filter, update);
            //}
            return Task.CompletedTask;


        }
        // <summary>
        /// Update subdocuments
        /// </summary>
        /// <typeparam name="T">Document</typeparam>
        /// <typeparam name="Z">Subdocuments</typeparam>
        /// <param name="id">Ident of entitie</param>
        /// <param name="field"></param>
        /// <param name="elemFieldMatch">Subdocument field to match</param>
        /// <param name="value">Subdocument - to update (all values)</param>
        /// <param name="updateMany">Update many records</param>
        /// <returns></returns>
        public virtual Task UpdateToSet<U>(Expression<Func<T, IEnumerable<U>>> field, U elemFieldMatch, U value, bool updateMany = false)
        {
            //TODO
            //MemberExpression me = field.Body as MemberExpression;
            //MemberInfo minfo = me.Member;

            //var filter = new BsonDocument
            //{
            //    new BsonElement(minfo.Name, elemFieldMatch.ToString())
            //};

            //var update = Builders<T>.Update.Set($"{minfo.Name}.$", value);
            //await _collection.UpdateManyAsync(filter, update);
            return Task.CompletedTask;

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
        public virtual Task PullFilter<U, Z>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, Z>> elemFieldMatch, Z elemMatch, bool updateMany = false)
        {
            //TODO
            //var filter = string.IsNullOrEmpty(id) ? Builders<T>.Filter.Where(x => true) : Builders<T>.Filter.Eq(x => x.Id, id);
            //var update = Builders<T>.Update.PullFilter(field, Builders<U>.Filter.Eq(elemFieldMatch, elemMatch));
            //if (updateMany)
            //{
            //    await _collection.UpdateManyAsync(filter, update);
            //}
            //else
            //{
            //    await _collection.UpdateOneAsync(filter, update);
            //}
            return Task.CompletedTask;

        }

        /// <summary>
        /// Delete subdocument
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="id"></param>
        /// <param name="field"></param>
        /// <param name="elemFieldMatch"></param>
        /// <returns></returns>
        public virtual Task PullFilter<U>(string id, Expression<Func<T, IEnumerable<U>>> field, Expression<Func<U, bool>> elemFieldMatch)
        {
            //TODO
            //var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            //var update = Builders<T>.Update.PullFilter(field, elemFieldMatch);
            //await _collection.UpdateOneAsync(filter, update);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Delete subdocument
        /// </summary>
        /// <param name="id"></param>
        /// <param name="field"></param>
        /// <param name="elemMatch"></param>
        /// <returns></returns>
        public virtual Task Pull(string id, Expression<Func<T, IEnumerable<string>>> field, string element, bool updateMany = false)
        {
            //TODO
            //var filter = string.IsNullOrEmpty(id) ? Builders<T>.Filter.Where(x => true) : Builders<T>.Filter.Eq(x => x.Id, id);
            //var update = Builders<T>.Update.Pull(field, element);
            //if (updateMany)
            //{
            //    await _collection.UpdateManyAsync(filter, update);
            //}
            //else
            //{
            //    await _collection.UpdateOneAsync(filter, update);
            //}
            return Task.CompletedTask;

        }
        /// <summary>
        /// Async Update entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual async Task<IEnumerable<T>> UpdateAsync(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                await UpdateAsync(entity);
            }
            return entities;
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual void Delete(T entity)
        {
            _collection.Delete(new BsonValue(entity.Id));
        }

        /// <summary>
        /// Async Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public virtual async Task<T> DeleteAsync(T entity)
        {
            Delete(entity);
            return await Task.FromResult(entity);
        }

        /// <summary>
        /// Delete entities
        /// </summary>
        /// <param name="entities">Entities</param>
        public virtual void Delete(IEnumerable<T> entities)
        {
            foreach (T entity in entities)
            {
                Delete(entity);
            }
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
        public virtual Task DeleteManyAsync(Expression<Func<T, bool>> filterexpression)
        {
            _collection.DeleteMany(filterexpression);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear entities
        /// </summary>
        public Task ClearAsync()
        {
            _collection.DeleteAll();
            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a table
        /// </summary>
        public virtual IQueryable<T> Table {
            get { return _collection.Query().ToEnumerable().AsQueryable(); }
        }

        /// <summary>
        /// Gets a table collection
        /// </summary>
        public virtual IQueryable<T> TableCollection(string collectionName)
        {
            return _database.GetCollection<T>(collectionName).Query().ToEnumerable().AsQueryable();
        }

        #endregion
    }
}