using Grand.Business.Common.Interfaces.Security;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Common.Services.Security
{
    /// <summary>
    /// ACL service
    /// </summary>
    public partial class AclService : IAclService
    {
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public AclService()
        {

        }

        #endregion

        #region Methods


        /// <summary>
        /// Authorize ACL permission
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Wntity</param>
        /// <param name="customer">Customer</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize<T>(T entity, Customer customer) where T : BaseEntity, IGroupLinkEntity
        {
            if (entity == null)
                return false;

            if (customer == null)
                return false;

            if (!entity.LimitedToGroups)
                return true;

            if (CommonHelper.IgnoreAcl)
                return true;

            foreach (var role1 in customer.Groups)
                foreach (var role2Id in entity.CustomerGroups)
                    if (role1 == role2Id)
                        //yes, we have such permission
                        return true;

            //no permission found
            return false;
        }

        /// <summary>
        /// Authorize whether entity could be accessed in a store (mapped to this store)
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="entity">Entity</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize<T>(T entity, string storeId) where T : BaseEntity, IStoreLinkEntity
        {
            if (entity == null)
                return false;

            if (string.IsNullOrEmpty(storeId))
                //return true if no store specified/found
                return true;

            if (CommonHelper.IgnoreStoreLimitations)
                return true;

            if (!entity.LimitedToStores)
                return true;

            foreach (var storeIdWithAccess in entity.Stores)
                if (storeId == storeIdWithAccess)
                    //yes, we have such permission
                    return true;

            //no permission found
            return false;
        }

        #endregion
    }
}