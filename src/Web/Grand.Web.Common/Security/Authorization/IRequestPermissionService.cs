using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;

namespace Grand.Web.Common.Security.Authorization;

/// <summary>
///     Permission service for code that runs inside a request
/// </summary>
public interface IRequestPermissionService : IPermissionService
{
    /// <summary>
    ///     Authorizes the customer of the current request.
    /// </summary>
    /// <param name="permission">Permission</param>
    /// <returns>true - authorized; otherwise, false</returns>
    Task<bool> Authorize(Permission permission);

    /// <summary>
    ///     Authorizes the customer of the current request.
    /// </summary>
    /// <param name="permissionSystemName">Permission system name</param>
    /// <returns>true - authorized; otherwise, false</returns>
    Task<bool> Authorize(string permissionSystemName);
}
