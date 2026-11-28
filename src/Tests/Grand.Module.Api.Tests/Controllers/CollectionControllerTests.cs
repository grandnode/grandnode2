using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.Controllers;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Queries.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Controllers;

[TestClass]
public class CollectionControllerTests
{
    private Mock<IMediator> _mediatorMock;
    private CollectionController _controller;

    public CollectionControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var customer = new Customer { Id = "admin" };
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.Authorize(PermissionSystemName.Collections, customer)).ReturnsAsync(true);
        permissionService.Setup(p => p.AuthorizeAction(PermissionSystemName.Collections, PermissionActionName.Edit, customer))
            .ReturnsAsync(true);

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var services = new ServiceCollection().AddSingleton(contextAccessor.Object).BuildServiceProvider();

        _controller = new CollectionController(_mediatorMock.Object, permissionService.Object) {
            ControllerContext = new ControllerContext
                { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }

    private void SetupStoredCollection(CollectionDto? collection)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGenericQuery<CollectionDto, Collection>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((collection is null ? [] : new[] { collection }).AsQueryable());
    }

    [TestMethod]
    public async Task Get_StoredCollection_ReturnsTheCollectionItself()
    {
        var stored = new CollectionDto { Id = "c-1", Name = "Summer" };
        SetupStoredCollection(stored);

        var result = await _controller.Get("c-1");

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreSame(stored, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    public async Task Get_UnknownCollection_ReturnsNotFound()
    {
        SetupStoredCollection(null);

        var result = await _controller.Get("missing");

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Put_UnknownCollection_ReturnsNotFound()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCollectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionDto?)null);

        var result = await _controller.Put(new CollectionDto { Id = "missing", Name = "Summer" });

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Put_StoredCollection_ReturnsTheUpdatedCollection()
    {
        var updated = new CollectionDto { Id = "c-1", Name = "Summer" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCollectionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.Put(new CollectionDto { Id = "c-1", Name = "Summer" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        Assert.AreSame(updated, ((OkObjectResult)result).Value);
    }

    [TestMethod]
    public async Task Put_WithoutId_ReturnsBadRequest()
    {
        var result = await _controller.Put(new CollectionDto { Name = "Summer" });

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateCollectionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static JsonPatchDocument<CollectionDto> Replace(string path, object value)
    {
        var patch = new JsonPatchDocument<CollectionDto>();
        patch.Operations.Add(new Operation<CollectionDto>("replace", path, null, value));
        return patch;
    }

    [TestMethod]
    public async Task Patch_UnknownPath_BadRequest_NothingSaved()
    {
        SetupStoredCollection(new CollectionDto { Id = "c-1", Name = "Summer" });

        var result = await _controller.Patch("c-1", Replace("/NoSuchField", "x"));

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateCollectionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Patch_KnownPath_SavesTheChange()
    {
        var stored = new CollectionDto { Id = "c-1", Name = "Summer" };
        SetupStoredCollection(stored);

        var result = await _controller.Patch("c-1", Replace("/Name", "Winter"));

        Assert.IsInstanceOfType(result, typeof(OkResult));
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateCollectionCommand>(c => c.Model.Name == "Winter"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
