using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class BackOfficeAccountExtensionsTests
{
    private static Mock<IPermissionService> Permissions(bool adminPanel)
    {
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, It.IsAny<Customer>()))
            .ReturnsAsync(adminPanel);
        return permissions;
    }

    [TestMethod]
    public async Task Customer_NotBackOffice() =>
        Assert.IsFalse(await Permissions(false).Object.IsBackOfficeAccount(new Customer()));

    [TestMethod]
    public async Task AdminPanelAccess_BackOffice() =>
        Assert.IsTrue(await Permissions(true).Object.IsBackOfficeAccount(new Customer()));

    [TestMethod]
    public async Task Vendor_BackOffice() =>
        Assert.IsTrue(await Permissions(false).Object.IsBackOfficeAccount(new Customer { VendorId = "v1" }));

    [TestMethod]
    public async Task StoreStaff_BackOffice() =>
        Assert.IsTrue(await Permissions(false).Object.IsBackOfficeAccount(new Customer { StaffStoreId = "s1" }));
}
