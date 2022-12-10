using Grand.Domain.Customers;
using Grand.Domain.Permissions;

namespace Grand.Business.Core.Interfaces.Common.Security
{
    /// <summary>
    /// Permission service interface
    /// </summary>
    public interface IPermissionService
    {
       
        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="permissionId">Permission identifier</param>
        /// <returns>Permission</returns>
        Task<Permission> GetPermissionById(string permissionId);

        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="systemName">Permission system name</param>
        /// <returns>Permission</returns>
        Task<Permission> GetPermissionBySystemName(string systemName);

        /// <summary>
        /// Gets all permissions
        /// </summary>
        /// <returns>Permissions</returns>
        Task<IList<Permission>> GetAllPermissions();

        /// <summary>
        /// Inserts a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        Task InsertPermission(Permission permission);

        /// <summary>
        /// Updates the permission
        /// </summary>
        /// <param name="permission">Permission</param>
        Task UpdatePermission(Permission permission);

        /// <summary>
        /// Delete a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        Task DeletePermission(Permission permission);

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> Authorize(Permission permission);

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission</param>
        /// <param name="customer">Customer</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> Authorize(Permission permission, Customer customer);

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionSystemName">Permission system name</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> Authorize(string permissionSystemName);

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionSystemName">Permission system name</param>
        /// <param name="customer">Customer</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> Authorize(string permissionSystemName, Customer customer);

        /// <summary>
        /// Gets a permission actions
        /// </summary>
        /// <param name="systemName">Permission system name</param>
        /// <param name="customerGroupId">Customer group ident</param>
        /// <returns>Permission action</returns>
        Task<IList<PermissionAction>> GetPermissionActions(string systemName, string customerGroupId);

        /// <summary>
        /// Inserts a permission action
        /// </summary>
        /// <param name="permissionAction">Permission</param>
        Task InsertPermissionAction(PermissionAction permissionAction);

        /// <summary>
        /// Inserts a permission action record
        /// </summary>
        /// <param name="permissionAction">Permission</param>
        Task DeletePermissionAction(PermissionAction permissionAction);

        /// <summary>
        /// Authorize permission for action
        /// </summary>
        /// <param name="permissionSystemName">Permission record system name</param>
        /// <param name="permissionActionName">Permission action name</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> AuthorizeAction(string permissionSystemName, string permissionActionName);

    }
}