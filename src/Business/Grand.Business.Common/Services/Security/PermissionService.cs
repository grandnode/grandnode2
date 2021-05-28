using Grand.Business.Common.Interfaces.Security;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.Common.Interfaces.Directory;

namespace Grand.Business.Common.Services.Security
{
    /// <summary>
    /// Permission service
    /// </summary>
    public partial class PermissionService : IPermissionService
    {
        #region Fields

        private readonly IRepository<Permission> _permissionRepository;
        private readonly IRepository<PermissionAction> _permissionActionRepository;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="permissionRepository">Permission repository</param>
        /// <param name="permissionActionRepository">Permission action repository</param>
        /// <param name="workContext">Work context</param>
        /// <param name="groupService">Group service</param>
        /// <param name="cacheBase">Cache manager</param>
        public PermissionService(
            IRepository<Permission> permissionRepository,
            IRepository<PermissionAction> permissionActionRepository,
            IWorkContext workContext,
            IGroupService groupService,
            ICacheBase cacheBase)
        {
            _permissionRepository = permissionRepository;
            _permissionActionRepository = permissionActionRepository;
            _workContext = workContext;
            _groupService = groupService;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionSystemName">Permission system name</param>
        /// <param name="customerGroup">Customer group</param>
        /// <returns>true - authorized; otherwise, false</returns>
        protected virtual async Task<bool> Authorize(string permissionSystemName, CustomerGroup customerGroup)
        {
            if (string.IsNullOrEmpty(permissionSystemName))
                return false;

            string key = string.Format(CacheKey.PERMISSIONS_ALLOWED_KEY, customerGroup.Id, permissionSystemName);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var permissionRecord = await Task.FromResult(_permissionRepository.Table.Where(x => x.SystemName == permissionSystemName).FirstOrDefault());
                return permissionRecord?.CustomerGroups.Contains(customerGroup.Id) ?? false;
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual async Task DeletePermission(Permission permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            await _permissionRepository.DeleteAsync(permission);

            await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="permissionId">Permission identifier</param>
        /// <returns>Permission</returns>
        public virtual Task<Permission> GetPermissionById(string permissionId)
        {
            return _permissionRepository.GetByIdAsync(permissionId);
        }

        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="systemName">Permission system name</param>
        /// <returns>Permission</returns>
        public virtual async Task<Permission> GetPermissionBySystemName(string systemName)
        {
            if (String.IsNullOrWhiteSpace(systemName))
                return await Task.FromResult<Permission>(null);

            var query = from pr in _permissionRepository.Table
                        where pr.SystemName == systemName
                        select pr;

            return await Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Gets all permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual async Task<IList<Permission>> GetAllPermissions()
        {
            var query = from pr in _permissionRepository.Table
                        orderby pr.Name
                        select pr;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Inserts a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual async Task InsertPermission(Permission permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            await _permissionRepository.InsertAsync(permission);

            await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual async Task UpdatePermission(Permission permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            await _permissionRepository.UpdateAsync(permission);

            await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission record</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual async Task<bool> Authorize(Permission permission)
        {
            return await Authorize(permission, _workContext.CurrentCustomer);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission</param>
        /// <param name="customer">Customer</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual async Task<bool> Authorize(Permission permission, Customer customer)
        {
            if (permission == null)
                return false;

            if (customer == null)
                return false;

            return await Authorize(permission.SystemName, customer);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionSystemName">Permission system name</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual async Task<bool> Authorize(string permissionSystemName)
        {
            return await Authorize(permissionSystemName, _workContext.CurrentCustomer);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionSystemName">Permission system name</param>
        /// <param name="customer">Customer</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual async Task<bool> Authorize(string permissionSystemName, Customer customer)
        {
            if (String.IsNullOrEmpty(permissionSystemName))
                return false;

            var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
            foreach (var group in customerGroups)
                if (await Authorize(permissionSystemName, group))
                    //yes, we have such permission
                    return true;

            //no permission found
            return false;
        }

        /// <summary>
        /// Gets a permission action
        /// </summary>
        /// <param name="systemName">Permission system name</param>
        /// <param name="customeroleId">Customer group ident</param>
        /// <returns>Permission action</returns>
        public virtual async Task<IList<PermissionAction>> GetPermissionActions(string systemName, string customeroleId)
        {
            return await Task.FromResult(_permissionActionRepository.Table
                    .Where(x => x.SystemName == systemName && x.CustomerGroupId == customeroleId).ToList());
        }

        /// <summary>
        /// Inserts a permission action record
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual async Task InsertPermissionAction(PermissionAction permissionAction)
        {
            if (permissionAction == null)
                throw new ArgumentNullException(nameof(permissionAction));

            //insert
            await _permissionActionRepository.InsertAsync(permissionAction);
            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Inserts a permission action record
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual async Task DeletePermissionAction(PermissionAction permissionAction)
        {
            if (permissionAction == null)
                throw new ArgumentNullException(nameof(permissionAction));

            //delete
            await _permissionActionRepository.DeleteAsync(permissionAction);
            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Authorize permission for action
        /// </summary>
        /// <param name="permissionRecordSystemName">Permission system name</param>
        /// <param name="permissionActionName">Permission action name</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual async Task<bool> AuthorizeAction(string permissionSystemName, string permissionActionName)
        {
            if (string.IsNullOrEmpty(permissionSystemName) || string.IsNullOrEmpty(permissionActionName))
                return false;

            if (!await Authorize(permissionSystemName))
                return false;

            var customerGroups = await _groupService.GetAllByIds(_workContext.CurrentCustomer.Groups.ToArray());
            foreach (var group in customerGroups)
            {
                if (!await Authorize(permissionSystemName, group))
                    continue;

                var key = string.Format(CacheKey.PERMISSIONS_ALLOWED_ACTION_KEY, group.Id, permissionSystemName, permissionActionName);
                var permissionAction = await _cacheBase.GetAsync(key, async () =>
                {
                    return await Task.FromResult(_permissionActionRepository.Table.Where(x => x.SystemName == permissionSystemName && x.CustomerGroupId == group.Id && x.Action == permissionActionName)
                                .FirstOrDefault());
                });
                if (permissionAction != null)
                    return false;
            }

            return true;
        }

        #endregion
    }
}