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
            var entity = _database.GetCollection(typeof(T).Name).FindById(new(id));
            entity[GetName(expression)] = new BsonValue(value);
            _database.GetCollection(typeof(T).Name).Update(entity);

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
            var entity = _collection.FindOne(filterexpression);
            Update(entity, updateBuilder);
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
            var entities = _collection.Find(filterexpression);
            foreach (var entity in entities)
            {
                Update(entity, updateBuilder);
            }
            return Task.CompletedTask;
        }

        private Task Update(T entity, UpdateBuilder<T> updateBuilder)
        {
            foreach (var item in updateBuilder.ExpressionFields)
            {
                var name = GetName(item.Expression);

                var propertyInfo = entity?.GetType().GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance);

                propertyInfo.SetValue(entity, item.Value);
            }
            _collection.Update(entity);
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
            var collection = _database.GetCollection(_collection.Name);
            var entity = collection.FindById(new(id));
            var fieldName = ((MemberExpression)field.Body).Member.Name;

            if (entity[fieldName].IsArray)
            {
                var bsonValue = BsonMapper.Global.Serialize<U>(value);
                var list = entity[fieldName].AsArray.ToList();
                list.Add(bsonValue);
                entity[fieldName] = new BsonArray(list);
                collection.Update(entity);
            }
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
            var collection = _database.GetCollection(_collection.Name);
            var entity = collection.FindById(new(id));
            var fieldName = ((MemberExpression)field.Body).Member.Name;
            var elementfieldName = ((MemberExpression)elemFieldMatch.Body).Member.Name;

            if (entity[fieldName].IsArray)
            {
                var bsonValue = BsonMapper.Global.Serialize<U>(value);
                var list = entity[fieldName].AsArray.ToList();
                var document = list.FirstOrDefault(x => x[elementfieldName] == new BsonValue(elemMatch));
                if (document == null) return Task.CompletedTask;

                foreach (var key in bsonValue.AsDocument.Keys)
                {
                    document[key] = bsonValue[key];
                }
                entity[fieldName] = new BsonArray(list);
                collection.Update(entity);
            }

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
            var collection = _database.GetCollection(_collection.Name);
            var entity = collection.FindById(new(id));
            var fieldName = ((MemberExpression)field.Body).Member.Name;
            if (entity == null) return Task.CompletedTask;

            var elements = entity.GetType().GetProperty(fieldName).GetValue(entity, null) as List<U>;

            var position = elements.FirstOrDefault(elemFieldMatch.Compile());

            if (position == null) return Task.CompletedTask;

            foreach (var item in position.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyField = position.GetType().GetProperty(item.Name,
                    BindingFlags.Public | BindingFlags.Instance);

                propertyField.SetValue(position, item.GetValue(value));
            }

            var propertyInfo = entity?.GetType().GetProperty(fieldName,
                    BindingFlags.Public | BindingFlags.Instance);

            propertyInfo.SetValue(entity, elements);

            collection.Update(entity);

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
        public virtual Task UpdateToSet<U>(Expression<Func<T, IEnumerable<U>>> field, U elemFieldMatch, U value)
        {
            var collection = _database.GetCollection(_collection.Name);
            var fieldName = ((MemberExpression)field.Body).Member.Name;
            var records = collection.Find(Query.EQ($"{fieldName}[*] ANY", elemFieldMatch.ToString())).ToList();
            foreach (var entity in records)
            {
                if (entity[fieldName].IsArray)
                {
                    var bsonValue = BsonMapper.Global.Serialize<U>(value);
                    var oldbsonValue = BsonMapper.Global.Serialize<U>(elemFieldMatch);
                    var list = entity[fieldName].AsArray.ToList();
                    list.Add(bsonValue);
                    list.Remove(oldbsonValue);
                    entity[fieldName] = new BsonArray(list);
                    collection.Update(entity);
                }
            }
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

        #region Helpers
        protected string GetName(LambdaExpression lambdaexpression)
        {
            var expression = (MemberExpression)lambdaexpression.Body;
            return expression.Member.Name;
        }
        protected string GetName<TSource, TField>(Expression<Func<TSource, TField>> Field)
        {
            if (object.Equals(Field, null))
            {
                throw new NullReferenceException("Field is required");
            }

            MemberExpression expr = null;

            if (Field.Body is MemberExpression)
            {
                expr = (MemberExpression)Field.Body;
            }
            else if (Field.Body is UnaryExpression)
            {
                expr = (MemberExpression)((UnaryExpression)Field.Body).Operand;
            }
            else
            {
                const string Format = "Expression '{0}' not supported.";
                string message = string.Format(Format, Field);

                throw new ArgumentException(message, "Field");
            }

            return expr.Member.Name;
        }

        #endregion
    }
}