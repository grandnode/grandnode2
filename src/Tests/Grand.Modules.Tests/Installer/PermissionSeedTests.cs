using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Module.Installer.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     The installer seeds permissions from its own list, not from PermissionProvider, so a new permission
///     has to be added there too or fresh installations never get it.
/// </summary>
[TestClass]
public class PermissionSeedTests
{
    [TestMethod]
    public void InstallPermissions_IncludeManageAuditLog()
    {
        Assert.IsTrue(PermissionExtensions.Permissions().Any(p => p.SystemName == PermissionSystemName.AuditLog));
    }

    [TestMethod]
    public void InstallDefaultPermissions_OnlyAdministratorsGetManageAuditLog()
    {
        var holders = PermissionExtensions.DefaultPermissions()
            .Where(d => d.Permissions.Any(p => p.SystemName == PermissionSystemName.AuditLog))
            .Select(d => d.CustomerGroupSystemName).ToList();

        CollectionAssert.AreEqual(new[] { SystemCustomerGroupNames.Administrators }, holders);
    }
}
