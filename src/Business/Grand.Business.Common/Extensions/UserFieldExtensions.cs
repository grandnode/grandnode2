using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain;
using System;
using System.Threading.Tasks;

namespace Grand.Business.Common.Extensions
{
    public static class UserFieldExtensions
    {
        /// <summary>
        /// Get an user field of an entity
        /// </summary>
        /// <typeparam name="TPropType">Property type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="userFieldService">UserFieldService</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Load a value specific for a certain store; pass 0 to load a value shared for all stores</param>
        /// <returns>Attribute</returns>
        public static async Task<TPropType> GetUserField<TPropType>(this BaseEntity entity, IUserFieldService userFieldService, string key, string storeId = "")
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return await userFieldService.GetFieldsForEntity<TPropType>(entity, key, storeId);
        }
    }
}
