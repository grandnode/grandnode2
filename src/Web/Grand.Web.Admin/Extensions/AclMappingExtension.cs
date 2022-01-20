using Grand.Domain;
using Grand.Domain.Stores;

namespace Grand.Web.Admin.Extensions
{
    public static class AclMappingExtension
    {
        /// <summary>
        /// Authorize whether entity could be accessed in a store 
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public static bool AccessToEntityByStore<T>(this T entity, string storeId) where T : BaseEntity, IStoreLinkEntity
        {
            if (entity == null)
                return false;

            if (string.IsNullOrEmpty(storeId))
                //return true if no store specified/found
                return true;

            if (entity.LimitedToStores && entity.Stores.Where(x => x == storeId).Any() && entity.Stores.Count == 1)
                //yes, we have such permission
                return true;

            //no permission found
            return false;
        }
    }
}
