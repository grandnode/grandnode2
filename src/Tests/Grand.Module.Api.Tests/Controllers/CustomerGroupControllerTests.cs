using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.Controllers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Queries.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Controllers;

[TestClass]
public class CustomerGroupControllerTests
{
    private Mock<IMediator> _mediatorMock;
    private CustomerGroupController _controller;

    public CustomerGroupControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var customer = new Customer { Id = "admin" };
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.Authorize(PermissionSystemName.Customers, customer)).ReturnsAsync(true);
        permissionService.Setup(p => p.AuthorizeAction(PermissionSystemName.Customers, PermissionActionName.Edit,
            customer)).ReturnsAsync(true);

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var services = new ServiceCollection().AddSingleton(contextAccessor.Object).BuildServiceProvider();

        _controller = new CustomerGroupController(_mediatorMock.Object, permissionService.Object) {
            ControllerContext = new ControllerContext
                { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }

    private void SetupStoredGroup(CustomerGroupDto? group)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGenericQuery<CustomerGroupDto, CustomerGroup>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((group is null ? [] : new[] { group }).AsQueryable());
    }

    [TestMethod]
    public async Task Put_StoredSystemGroup_ReturnsBadRequestWithoutUpdating()
    {
        SetupStoredGroup(new CustomerGroupDto { Id = "cg-1", Name = "Administrators", IsSystem = true });

        var result = await _controller.Put(new CustomerGroupDto { Id = "cg-1", Name = "Renamed", IsSystem = false });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateCustomerGroupCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Put_StoredGroupIsNotSystem_Updates()
    {
        SetupStoredGroup(new CustomerGroupDto { Id = "cg-2", Name = "VIP" });

        var result = await _controller.Put(new CustomerGroupDto { Id = "cg-2", Name = "VIP Customers" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateCustomerGroupCommand>(c => c.Model.Id == "cg-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Put_UnknownGroup_ReturnsNotFound()
    {
        SetupStoredGroup(null);

        var result = await _controller.Put(new CustomerGroupDto { Id = "cg-missing", Name = "VIP" });

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Put_WithoutId_ReturnsBadRequest()
    {
        var result = await _controller.Put(new CustomerGroupDto { Name = "VIP" });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateCustomerGroupCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Patch_StoredSystemGroup_ReturnsBadRequestWithoutUpdating()
    {
        SetupStoredGroup(new CustomerGroupDto { Id = "cg-1", Name = "Administrators", IsSystem = true });
        var patch = new JsonPatchDocument<CustomerGroupDto>();
        patch.Replace(x => x.IsSystem, false);

        var result = await _controller.Patch("cg-1", patch);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateCustomerGroupCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
