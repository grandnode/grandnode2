using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Security.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Authorization;

[TestClass]
public class RequestPermissionServiceTests
{
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<RequestPermissionService> _serviceMock;
    private RequestPermissionService _service;

    [TestInitialize]
    public void Init()
    {
        _contextAccessorMock = new Mock<IContextAccessor>();
        _serviceMock = new Mock<RequestPermissionService>(Mock.Of<IRepository<Permission>>(),
            Mock.Of<IRepository<PermissionAction>>(), Mock.Of<IGroupService>(), Mock.Of<ICacheBase>(),
            _contextAccessorMock.Object) { CallBase = true };
        _service = _serviceMock.Object;
    }

    [TestMethod]
    public async Task Authorize_SystemName_UsesCustomerOfTheRequest()
    {
        var customer = new Customer();
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _serviceMock.Setup(x => x.Authorize("ManageProducts", customer)).ReturnsAsync(true);

        Assert.IsTrue(await _service.Authorize("ManageProducts"));
    }

    [TestMethod]
    public async Task Authorize_Permission_UsesCustomerOfTheRequest()
    {
        var customer = new Customer();
        var permission = new Permission { SystemName = "ManageProducts" };
        _contextAccessorMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(customer);
        _serviceMock.Setup(x => x.Authorize(permission, customer)).ReturnsAsync(true);

        Assert.IsTrue(await _service.Authorize(permission));
    }

    [TestMethod]
    public async Task Authorize_WithoutWorkContext_ReturnsFalse()
    {
        _contextAccessorMock.Setup(c => c.WorkContext).Returns((IWorkContext)null);

        Assert.IsFalse(await _service.Authorize("ManageProducts"));
        Assert.IsFalse(await _service.Authorize(new Permission { SystemName = "ManageProducts" }));
    }

    [TestMethod]
    public async Task Authorize_WithCustomer_DoesNotReadTheRequest()
    {
        var customer = new Customer();
        _serviceMock.Setup(x => x.Authorize("ManageProducts", customer)).ReturnsAsync(true);

        Assert.IsTrue(await _service.Authorize("ManageProducts", customer));
        _contextAccessorMock.VerifyNoOtherCalls();
    }
}
