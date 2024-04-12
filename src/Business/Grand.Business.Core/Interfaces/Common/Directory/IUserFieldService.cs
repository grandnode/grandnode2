using Grand.Domain;

namespace Grand.Business.Core.Interfaces.Common.Directory;

/// <summary>
///     User fields service interface
/// </summary>
public interface IUserFieldService
{
    Task SaveField<TPropType>(BaseEntity entity, string key, TPropType value, string storeId = "");
    Task<TPropType> GetFieldsForEntity<TPropType>(BaseEntity entity, string key, string storeId = "");
}