using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Security.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Authorization;

[TestClass]
public class RequestPermissionServiceTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private RequestPermissionService _service;

    [TestInitialize]
    public void Init()
    {
        _contextAccessorMock = new Mock<IContextAccessor>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _service = new RequestPermissionService(_permissionServiceMock.Object, _contextAccessorMock.Object);
    }

    [TestMethod]
    public async Task Authorize_SystemName_UsesCustomerOfTheRequest()
    {
        var customer = new Customer();
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _permissionServiceMock.Setup(x => x.Authorize("ManageProducts", customer)).ReturnsAsync(true);

        Assert.IsTrue(await _service.Authorize("ManageProducts"));
    }

    [TestMethod]
    public async Task Authorize_Permission_UsesCustomerOfTheRequest()
    {
        var customer = new Customer();
        var permission = new Permission { SystemName = "ManageProducts" };
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _permissionServiceMock.Setup(x => x.Authorize(permission, customer)).ReturnsAsync(true);

        Assert.IsTrue(await _service.Authorize(permission));
    }

    [TestMethod]
    public async Task Authorize_WithoutWorkContext_PassesNoCustomer()
    {
        _contextAccessorMock.Setup(c => c.WorkContext).Returns((IWorkContext)null);

        Assert.IsFalse(await _service.Authorize("ManageProducts"));
        Assert.IsFalse(await _service.Authorize(new Permission { SystemName = "ManageProducts" }));
        _permissionServiceMock.Verify(x => x.Authorize("ManageProducts", null), Times.Once);
        _permissionServiceMock.Verify(x => x.Authorize(It.IsAny<Permission>(), null), Times.Once);
    }
}
