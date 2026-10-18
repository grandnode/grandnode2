using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;

namespace Grand.Web.Common.Security.Authorization;

public class RequestPermissionService(
    IPermissionService permissionService,
    IContextAccessor contextAccessor) : IRequestPermissionService
{
    public Task<bool> Authorize(Permission permission)
    {
        return permissionService.Authorize(permission, contextAccessor.WorkContext?.CurrentCustomer);
    }

    public Task<bool> Authorize(string permissionSystemName)
    {
        return permissionService.Authorize(permissionSystemName, contextAccessor.WorkContext?.CurrentCustomer);
    }
}
