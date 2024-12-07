namespace Grand.Domain.Permissions;

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