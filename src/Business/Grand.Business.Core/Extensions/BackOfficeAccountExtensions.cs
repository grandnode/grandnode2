using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;

namespace Grand.Business.Core.Extensions;

public static class BackOfficeAccountExtensions
{
    /// <summary>
    ///     A back-office account: vendor, store staff, or an account with admin panel access
    /// </summary>
    public static async Task<bool> IsBackOfficeAccount(this IPermissionService permissionService, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return !string.IsNullOrEmpty(customer.VendorId) || !string.IsNullOrEmpty(customer.StaffStoreId) ||
               await permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer);
    }
}
