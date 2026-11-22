using Grand.Domain.Permissions;
using Grand.Domain.Stores;

namespace Grand.Web.Common.Security.Authorization;

public class StoreAccessService(
    IRequestPermissionService permissionService,
    StoreInformationSettings storeInformationSettings) : IStoreAccessService
{
    public async Task<StoreAccess> GetStoreAccess()
    {
        if (storeInformationSettings.StoreClosed &&
            !await permissionService.Authorize(StandardPermission.AccessClosedStore))
            return StoreAccess.Closed;

        return await permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation)
            ? StoreAccess.Open
            : StoreAccess.RegisteredOnly;
    }
}
