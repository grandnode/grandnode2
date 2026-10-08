using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcResourceAccessTests
{
    private Mock<IPermissionService> _permission;
    private Mock<IGroupService> _groups;
    private OidcResourceAccess _access;

    [TestInitialize]
    public void Init()
    {
        _permission = new Mock<IPermissionService>();
        _groups = new Mock<IGroupService>();
        _access = new OidcResourceAccess(_permission.Object, _groups.Object);
    }

    [TestMethod]
    public async Task NoFlags_Allowed() => Assert.IsNull(await _access.Refusal(new OidcProtectedResource(), new Customer()));

    [TestMethod]
    public async Task AdminPanel_RequiresPermission()
    {
        var customer = new Customer { Id = "c1" };
        var resource = new OidcProtectedResource { RequireAdminPanelAccess = true };
        _permission.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, customer)).ReturnsAsync(false);
        Assert.IsNotNull(await _access.Refusal(resource, customer));
        _permission.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, customer)).ReturnsAsync(true);
        Assert.IsNull(await _access.Refusal(resource, customer));
    }

    [TestMethod]
    public async Task DenyVendors_ByVendorIdOrGroup()
    {
        var resource = new OidcProtectedResource { DenyVendors = true };
        Assert.IsNotNull(await _access.Refusal(resource, new Customer { VendorId = "v1" }));
        var inGroup = new Customer { Id = "c2" };
        _groups.Setup(x => x.IsVendor(inGroup)).ReturnsAsync(true);
        Assert.IsNotNull(await _access.Refusal(resource, inGroup));
        Assert.IsNull(await _access.Refusal(resource, new Customer { Id = "c3" }));
    }

    [TestMethod]
    public async Task DenyStoreManagers_ByStaffStoreOrGroup()
    {
        var resource = new OidcProtectedResource { DenyStoreManagers = true };
        Assert.IsNotNull(await _access.Refusal(resource, new Customer { StaffStoreId = "s1" }));
        var inGroup = new Customer { Id = "c2" };
        _groups.Setup(x => x.IsStoreManager(inGroup)).ReturnsAsync(true);
        Assert.IsNotNull(await _access.Refusal(resource, inGroup));
        Assert.IsNull(await _access.Refusal(resource, new Customer { Id = "c3" }));
    }
}
