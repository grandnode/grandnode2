using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Module.Api.Controllers;
using Grand.Module.Api.DTOs.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Controllers;

[TestClass]
public class CustomerControllerSetPasswordTests
{
    private readonly Customer _current = new() { Id = "staff" };
    private readonly Customer _target = new() { Id = "c1", Email = "c1@shop.test" };
    private CustomerController _controller;
    private Mock<ICustomerManagerService> _customerManagerService;
    private Mock<IGroupService> _groupService;

    public CustomerControllerSetPasswordTests()
    {
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(x => x.Authorize(It.IsAny<string>(), _current)).ReturnsAsync(true);
        permissionService.Setup(x => x.AuthorizeAction(It.IsAny<string>(), It.IsAny<string>(), _current))
            .ReturnsAsync(true);
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(x => x.GetCustomerByEmail(_target.Email, It.IsAny<string>())).ReturnsAsync(_target);
        _customerManagerService = new Mock<ICustomerManagerService>();
        _groupService = new Mock<IGroupService>();

        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext.CurrentCustomer).Returns(_current);
        var services = new ServiceCollection().AddSingleton(contextAccessor.Object).BuildServiceProvider();

        _controller = new CustomerController(Mock.Of<IMediator>(), permissionService.Object,
            _customerManagerService.Object, new CustomerSettings(), customerService.Object, _groupService.Object) {
            ControllerContext = new ControllerContext
                { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }

    [TestMethod]
    public async Task NonAdministrator_SetsAnAdministratorsPassword_Forbidden()
    {
        _groupService.Setup(x => x.IsAdmin(_target)).ReturnsAsync(true);

        Assert.IsNotNull(_target.Email);
        var result = await _controller.SetPassword(_target.Email, new PasswordDto { Password = "secret" });

        Assert.IsInstanceOfType<ForbidResult>(result);
        _customerManagerService.Verify(x => x.ChangePassword(It.IsAny<Customer>(), It.IsAny<PasswordFormat>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Administrator_SetsAnAdministratorsPassword_Changed()
    {
        _groupService.Setup(x => x.IsAdmin(It.IsAny<Customer>())).ReturnsAsync(true);

        Assert.IsNotNull(_target.Email);
        var result = await _controller.SetPassword(_target.Email, new PasswordDto { Password = "secret" });

        Assert.IsInstanceOfType<OkObjectResult>(result);
        _customerManagerService.Verify(x => x.ChangePassword(_target, It.IsAny<PasswordFormat>(), "secret"),
            Times.Once);
    }

    [TestMethod]
    public async Task NonAdministrator_SetsACustomersPassword_Changed()
    {
        Assert.IsNotNull(_target.Email);
        var result = await _controller.SetPassword(_target.Email, new PasswordDto { Password = "secret" });

        Assert.IsInstanceOfType<OkObjectResult>(result);
        _customerManagerService.Verify(x => x.ChangePassword(_target, It.IsAny<PasswordFormat>(), "secret"),
            Times.Once);
    }

    [TestMethod]
    public async Task UnknownEmail_NotFound()
    {
        var result = await _controller.SetPassword("nobody@shop.test", new PasswordDto { Password = "secret" });

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }
}
