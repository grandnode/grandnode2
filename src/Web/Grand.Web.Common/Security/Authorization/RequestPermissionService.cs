using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;

namespace Grand.Web.Common.Security.Authorization;

public class RequestPermissionService(
    IRepository<Permission> permissionRepository,
    IRepository<PermissionAction> permissionActionRepository,
    IGroupService groupService,
    ICacheBase cacheBase,
    IContextAccessor contextAccessor)
    : PermissionService(permissionRepository, permissionActionRepository, groupService, cacheBase),
        IRequestPermissionService
{
    public Task<bool> Authorize(Permission permission)
    {
        return Authorize(permission, contextAccessor.WorkContext?.CurrentCustomer);
    }

    public Task<bool> Authorize(string permissionSystemName)
    {
        return Authorize(permissionSystemName, contextAccessor.WorkContext?.CurrentCustomer);
    }
}
