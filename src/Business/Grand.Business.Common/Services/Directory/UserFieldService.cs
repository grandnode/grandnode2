using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Common.Services.Directory;

/// <summary>
///     User field service
/// </summary>
public class UserFieldService : IUserFieldService
{
    #region Fields

    private readonly IRepository<UserFieldBaseEntity> _userFieldBaseEntityRepository;

    #endregion

    #region Ctor

    public UserFieldService(
        IRepository<UserFieldBaseEntity> userFieldBaseEntityRepository)
    {
        _userFieldBaseEntityRepository = userFieldBaseEntityRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Save attribute value
    /// </summary>
    /// <typeparam name="TPropType">Property type</typeparam>
    /// <param name="entity">Entity</param>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    /// <param name="storeId">Store identifier; pass "" if this attribute will be available for all stores</param>
    public virtual async Task SaveField<TPropType>(BaseEntity entity, string key, TPropType value, string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(key);

        var collectionName = entity.GetType().Name;

        _ = _userFieldBaseEntityRepository.SetCollection(collectionName);

        var baseFields = await _userFieldBaseEntityRepository.GetByIdAsync(entity.Id);

        var props = baseFields.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

        var prop = props.FirstOrDefault(ga =>
            ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

        var valueStr = CommonHelper.To<string>(value);

        if (prop != null)
        {
            if (string.IsNullOrWhiteSpace(valueStr))
            {
                //delete
                await _userFieldBaseEntityRepository.PullFilter(entity.Id, x => x.UserFields,
                    y => y.Key == prop.Key && y.StoreId == storeId);

                var entityProp = entity.UserFields.FirstOrDefault(x => x.Key == prop.Key && x.StoreId == storeId);
                if (entityProp != null)
                    entity.UserFields.Remove(entityProp);
            }
            else
            {
                //update
                prop.Value = valueStr;
                await _userFieldBaseEntityRepository.UpdateToSet(entity.Id, x => x.UserFields,
                    y => y.Key == prop.Key && y.StoreId == storeId, prop);

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
                    StoreId = storeId
                };
                await _userFieldBaseEntityRepository.AddToSet(entity.Id, x => x.UserFields, prop);

                entity.UserFields.Add(prop);
            }
        }
    }

    public virtual async Task<TPropType> GetFieldsForEntity<TPropType>(BaseEntity entity, string key,
        string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(entity);

        var collectionName = entity.GetType().Name;
        _ = _userFieldBaseEntityRepository.SetCollection(collectionName);

        var baseFields = await _userFieldBaseEntityRepository.GetByIdAsync(entity.Id);
        var props = baseFields?.UserFields;
        if (props == null)
            return default;
        props = props.Where(x => x.StoreId == storeId).ToList();
        if (!props.Any())
            return default;

        var prop = props.FirstOrDefault(ga =>
            ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (prop == null || string.IsNullOrEmpty(prop.Value))
            return default;

        return CommonHelper.To<TPropType>(prop.Value);
    }

    #endregion
}