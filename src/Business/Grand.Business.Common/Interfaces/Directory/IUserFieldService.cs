using Grand.Domain;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    /// <summary>
    /// User fields service interface
    /// </summary>
    public partial interface IUserFieldService
    {
        Task SaveField<TPropType>(BaseEntity entity, string key, TPropType value, string storeId = "");
        Task SaveField<TPropType>(string entity, string entityId, string key, TPropType value, string storeId = "");
        Task<TPropType> GetFieldsForEntity<TPropType>(BaseEntity entity, string key, string storeId = "");
    }
}