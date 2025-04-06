namespace Grand.Domain.Permissions;

/// <summary>
///     Standard permissions
/// </summary>
public static partial class StandardPermission
{
    //admin area permissions
    public static readonly Permission ManageAccessStoreManagerPanel = new() {
        Name = "Access store manager panel",
        SystemName = PermissionSystemName.AccessStorePanel,
        Area = "Store area",
        Category = "Access Store"
    };
}