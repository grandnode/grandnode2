using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
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

        private readonly IRepository<UserFieldBaseEntity> _userfieldBaseEntitRepository;
        #endregion

        #region Ctor
        public UserFieldService(
            IRepository<UserFieldBaseEntity> userfieldBaseEntitRepository)
        {
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

            _ = _userfieldBaseEntitRepository.SetCollection(entity);

            var basefields = await _userfieldBaseEntitRepository.GetByIdAsync(entityId);

            var props = basefields.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

            var prop = props.FirstOrDefault(ga =>
                ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

            var valueStr = CommonHelper.To<string>(value);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    //delete
                    await _userfieldBaseEntitRepository.PullFilter(entityId, x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId);

                }
                else
                {
                    //update
                    await _userfieldBaseEntitRepository.UpdateToSet(entityId, x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId, prop);
                }
            }
            else
            {
                prop = new UserField {
                    Key = key,
                    Value = valueStr,
                    StoreId = storeId,
                };
                await _userfieldBaseEntitRepository.AddToSet(entityId, x => x.UserFields, prop);
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

            var collectionName = entity.GetType().Name;

            _ = _userfieldBaseEntitRepository.SetCollection(collectionName);

            var basefields = await _userfieldBaseEntitRepository.GetByIdAsync(entity.Id);

            var props = basefields.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

            var prop = props.FirstOrDefault(ga =>
                ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

            var valueStr = CommonHelper.To<string>(value);

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    //delete
                    await _userfieldBaseEntitRepository.PullFilter(entity.Id, x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId);

                    var entityProp = entity.UserFields.FirstOrDefault(x => x.Key == prop.Key && x.StoreId == storeId);
                    if (entityProp != null)
                        entity.UserFields.Remove(entityProp);
                }
                else
                {
                    //update
                    prop.Value = valueStr;
                    await _userfieldBaseEntitRepository.UpdateToSet(entity.Id, x => x.UserFields, y => y.Key == prop.Key && y.StoreId == storeId, prop);

                    var entityProp = entity.UserFields.FirstOrDefault(x => x.Key == prop.Key && x.StoreId == storeId);
                    if (entityProp != null)
                        entityProp.Value = valueStr;

                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(valueStr))
                {
                    prop = new UserField {
                        Key = key,
                        Value = valueStr,
                        StoreId = storeId,
                    };
                    await _userfieldBaseEntitRepository.AddToSet(entity.Id, x => x.UserFields, prop);

                    entity.UserFields.Add(prop);
                }
            }
        }

        public virtual async Task<TPropType> GetFieldsForEntity<TPropType>(BaseEntity entity, string key, string storeId = "")
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var collectionName = entity.GetType().Name;
            _ = _userfieldBaseEntitRepository.SetCollection(collectionName);

            var basefields = await _userfieldBaseEntitRepository.GetByIdAsync(entity.Id);
            if (basefields == null)
                return default(TPropType);

            var props = basefields.UserFields;
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