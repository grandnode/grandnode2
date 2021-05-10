using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    /// <summary>
    /// User field service
    /// </summary>
    public partial class UserFieldService : IUserFieldService
    {

        #region Fields

        private readonly IRepository<BaseEntity> _baseRepository;
        private readonly IRepository<UserFieldBaseEntity> _userfieldBaseEntitRepository;
        #endregion

        #region Ctor
        public UserFieldService(
            IRepository<BaseEntity> baseRepository,
            IRepository<UserFieldBaseEntity> userfieldBaseEntitRepository)
        {
            _baseRepository = baseRepository;
            _userfieldBaseEntitRepository = userfieldBaseEntitRepository;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Save attribute value
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity name (collection name)</param>
        /// <param name="entityId">EntityId</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier; pass 0 if this attribute will be available for all stores</param>
        public virtual async Task SaveField<TPropType>(string entity, string entityId, string key, TPropType value, string storeId = "")
        {
            if (string.IsNullOrEmpty(entity))
                throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var collection = _baseRepository.Database.GetCollection<UserFieldBaseEntity>(entity);
            var query = _baseRepository.Database.GetCollection<UserFieldBaseEntity>(entity).Find(new BsonDocument("_id", entityId)).FirstOrDefault();

            var props = query.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

            var prop = props.FirstOrDefault(ga =>
                ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

            var valueStr = CommonHelper.To<string>(value);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    //delete
                    var builder = Builders<UserFieldBaseEntity>.Update;
                    var updatefilter = builder.PullFilter(x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId);
                    await collection.UpdateManyAsync(new BsonDocument("_id", entityId), updatefilter);
                }
                else
                {
                    //update
                    prop.Value = valueStr;
                    var builder = Builders<UserFieldBaseEntity>.Filter;
                    var filter = builder.Eq(x => x.Id, entityId);
                    filter = filter & builder.Where(x => x.UserFields.Any(y => y.Key == prop.Key && y.StoreId == storeId));
                    var update = Builders<UserFieldBaseEntity>.Update
                        .Set(x => x.UserFields.ElementAt(-1).Value, prop.Value);
                    await collection.UpdateManyAsync(filter, update);
                }
            }
            else
            {
                prop = new UserField
                {
                    Key = key,
                    Value = valueStr,
                    StoreId = storeId,
                };
                var updatebuilder = Builders<UserFieldBaseEntity>.Update;
                var update = updatebuilder.AddToSet(p => p.UserFields, prop);
                await collection.UpdateOneAsync(new BsonDocument("_id", entityId), update);
            }
        }

        /// <summary>
        /// Save attribute value
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="storeId">Store identifier; pass 0 if this attribute will be available for all stores</param>
        public virtual async Task SaveField<TPropType>(BaseEntity entity, string key, TPropType value, string storeId = "")
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            string keyGroup = entity.GetType().Name;

            var collection = _baseRepository.Database.GetCollection<UserFieldBaseEntity>(keyGroup);
            var query = _baseRepository.Database.GetCollection<UserFieldBaseEntity>(keyGroup).Find(new BsonDocument("_id", entity.Id)).FirstOrDefault();

            var props = query.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

            var prop = props.FirstOrDefault(ga =>
                ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

            var valueStr = CommonHelper.To<string>(value);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    //delete
                    var builder = Builders<UserFieldBaseEntity>.Update;
                    var updatefilter = builder.PullFilter(x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId);
                    await collection.UpdateManyAsync(new BsonDocument("_id", entity.Id), updatefilter);
                    var entityProp = entity.UserFields.FirstOrDefault(x => x.Key == prop.Key && x.StoreId == storeId);
                    if (entityProp != null)
                        entity.UserFields.Remove(entityProp);
                }
                else
                {
                    //update
                    prop.Value = valueStr;
                    var builder = Builders<UserFieldBaseEntity>.Filter;
                    var filter = builder.Eq(x => x.Id, entity.Id);
                    filter = filter & builder.Where(x => x.UserFields.Any(y => y.Key == prop.Key && y.StoreId == storeId));
                    var update = Builders<UserFieldBaseEntity>.Update
                        .Set(x => x.UserFields.ElementAt(-1).Value, prop.Value);

                    await collection.UpdateManyAsync(filter, update);

                    var entityProp = entity.UserFields.FirstOrDefault(x => x.Key == prop.Key && x.StoreId == storeId);
                    if (entityProp != null)
                        entityProp.Value = valueStr;

                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(valueStr))
                {
                    prop = new UserField
                    {
                        Key = key,
                        Value = valueStr,
                        StoreId = storeId,
                    };
                    var updatebuilder = Builders<UserFieldBaseEntity>.Update;
                    var update = updatebuilder.AddToSet(p => p.UserFields, prop);
                    await collection.UpdateOneAsync(new BsonDocument("_id", entity.Id), update);
                    entity.UserFields.Add(prop);
                }
            }
        }

        public virtual async Task<TPropType> GetFieldsForEntity<TPropType>(BaseEntity entity, string key, string storeId = "")
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collection = await _userfieldBaseEntitRepository.Database.GetCollection<UserFieldBaseEntity>(entity.GetType().Name)
                .FindAsync(new BsonDocument("_id", entity.Id));

            var props = collection.FirstOrDefault().UserFields;
            if (props == null)
                return default(TPropType);
            props = props.Where(x => x.StoreId == storeId).ToList();
            if (!props.Any())
                return default(TPropType);

            var prop = props.FirstOrDefault(ga =>
                ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (prop == null || string.IsNullOrEmpty(prop.Value))
                return default(TPropType);

            return CommonHelper.To<TPropType>(prop.Value);
        }

        #endregion
    }
}