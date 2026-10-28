using Grand.Business.Common.Services.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Grand.Business.Common.Tests.Services.Security;

[TestClass]
public class PermissionProviderTests
{
    private readonly PermissionProvider _provider = new();

    [TestMethod]
    public void StoreManager_DefaultPermissions_IncludeManageCustomers()
    {
        var storeManager = _provider.GetDefaultPermissions()
            .FirstOrDefault(p => p.CustomerGroupSystemName == SystemCustomerGroupNames.StoreManager);

        Assert.IsNotNull(storeManager);
        CollectionAssert.Contains(storeManager.Permissions.ToList(), StandardPermission.ManageCustomers);
    }

    [TestMethod]
    public void GetPermissions_ContainsManageAuditLog()
    {
        var permission = _provider.GetPermissions().Single(p => p.SystemName == PermissionSystemName.AuditLog);

        Assert.AreEqual("Admin area", permission.Area);
        CollectionAssert.AreEquivalent(
            new[] { PermissionActionName.List, PermissionActionName.Preview, PermissionActionName.Export, PermissionActionName.Edit },
            permission.Actions.ToArray());
    }

    [TestMethod]
    public void GetDefaultPermissions_OnlyAdministratorsHaveManageAuditLog()
    {
        var holders = _provider.GetDefaultPermissions()
            .Where(d => d.Permissions.Any(p => p.SystemName == PermissionSystemName.AuditLog))
            .Select(d => d.CustomerGroupSystemName).ToList();

        CollectionAssert.AreEqual(new[] { SystemCustomerGroupNames.Administrators }, holders);
    }
}
