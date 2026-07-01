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
}
