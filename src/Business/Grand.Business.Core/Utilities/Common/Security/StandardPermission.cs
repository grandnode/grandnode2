using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

/// <summary>
///     Standard permissions
/// </summary>
public static partial class StandardPermission
{
    //admin area permissions
    public static readonly Permission ManageAccessAdminPanel = new() {
        Name = "Access admin",
        SystemName = PermissionSystemName.AccessAdminPanel,
        Area = "Admin area",
        Category = "Access Admin"
    };
}