using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

/// <summary>
///     Standard permissions
/// </summary>
public static partial class StandardPermission
{
    //admin area permissions
    public static readonly Permission ManageAccessVendorPanel = new() {
        Name = "Access vendor panel",
        SystemName = PermissionSystemName.AccessVendorPanel,
        Area = "Vendor area",
        Category = "Access Vendor"
    };
}