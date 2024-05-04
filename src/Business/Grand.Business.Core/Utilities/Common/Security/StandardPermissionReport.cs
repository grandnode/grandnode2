using Grand.Domain.Permissions;

namespace Grand.Business.Core.Utilities.Common.Security;

public static partial class StandardPermission
{
    public static readonly Permission ManageReports = new() {
        Name = "Manage Reports",
        SystemName = PermissionSystemName.Reports,
        Area = "Admin area",
        Category = CategoryReport
    };

    private static string CategoryReport => "Reports";
}