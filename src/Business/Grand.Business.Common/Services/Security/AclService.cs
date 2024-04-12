using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;

namespace Grand.Business.Common.Services.Security;

/// <summary>
///     ACL service
/// </summary>
public class AclService : IAclService
{
    #region Fields

    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public AclService(AccessControlConfig accessControlConfig)
    {
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Authorize ACL permission
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="entity">Entity</param>
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

        return _accessControlConfig.IgnoreAcl ||
               customer.Groups.Any(role1 => entity.CustomerGroups.Any(role2Id => role1 == role2Id));
    }

    /// <summary>
    ///     Authorize whether entity could be accessed in a store (mapped to this store)
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

        if (_accessControlConfig.IgnoreStoreLimitations)
            return true;

        return !entity.LimitedToStores || entity.Stores.Any(storeIdWithAccess => storeId == storeIdWithAccess);
    }

    #endregion
}