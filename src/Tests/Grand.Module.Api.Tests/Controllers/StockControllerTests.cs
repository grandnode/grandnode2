using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Module.Api.Commands.Models.Inventory;
using Grand.Module.Api.Controllers;
using Grand.Module.Api.DTOs.Inventory;
using Grand.Module.Api.Queries.Models.Inventory;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Controllers;

[TestClass]
public class StockControllerTests
{
    private readonly Customer _customer = new() { Id = "erp" };
    private Mock<IMediator> _mediatorMock;
    private Mock<IPermissionService> _permissionServiceMock;
    private StockController _controller;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<ApplyStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockApplyResultDto>());
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetStockJournalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockJournalResultDto());
        _permissionServiceMock = new Mock<IPermissionService>();
        _permissionServiceMock.Setup(x => x.Authorize(PermissionSystemName.StockIntegration, _customer))
            .ReturnsAsync(true);

        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(_customer);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var services = new ServiceCollection().AddSingleton(contextAccessor.Object).BuildServiceProvider();

        _controller = new StockController(_mediatorMock.Object, _permissionServiceMock.Object) {
            ControllerContext = new ControllerContext
                { HttpContext = new DefaultHttpContext { RequestServices = services } }
        };
    }

    private static StockApplyRequestDto Request(int lines)
    {
        return new StockApplyRequestDto {
            Lines = Enumerable.Range(0, lines).Select(i => new StockApplyLineDto { Key = $"k{i}" }).ToList()
        };
    }

    [TestMethod]
    public async Task Apply_WithoutStockIntegrationPermission_Forbidden()
    {
        _permissionServiceMock.Setup(x => x.Authorize(PermissionSystemName.StockIntegration, _customer))
            .ReturnsAsync(false);

        Assert.IsInstanceOfType<ForbidResult>(await _controller.Apply(Request(1)));
        Assert.IsInstanceOfType<ForbidResult>(await _controller.Journal(null));
    }

    [TestMethod]
    public async Task Apply_1000Lines_Sent()
    {
        Assert.IsInstanceOfType<OkObjectResult>(await _controller.Apply(Request(1000)));
        _mediatorMock.Verify(x => x.Send(It.Is<ApplyStockCommand>(c => c.Lines.Count == 1000 && c.CustomerId == "erp"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Apply_1001Lines_BadRequest()
    {
        Assert.IsInstanceOfType<BadRequestObjectResult>(await _controller.Apply(Request(1001)));
        _mediatorMock.Verify(x => x.Send(It.IsAny<ApplyStockCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Apply_EmptyLine_BadRequest()
    {
        var request = Request(1);
        request.Lines.Add(null);

        Assert.IsInstanceOfType<BadRequestObjectResult>(await _controller.Apply(request));
    }

    [TestMethod]
    public async Task Journal_MovementTypesByNameOrNumber_Passed()
    {
        Assert.IsInstanceOfType<OkObjectResult>(await _controller.Journal(null,
            movementTypes: $"Shipment, {(int)InventoryMovementType.Api}"));

        _mediatorMock.Verify(x => x.Send(It.Is<GetStockJournalQuery>(q =>
            q.MovementTypes.Count == 2 && q.MovementTypes.Contains(InventoryMovementType.Shipment) &&
            q.MovementTypes.Contains(InventoryMovementType.Api) && q.CustomerId == "erp"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Journal_UnknownMovementType_BadRequest()
    {
        Assert.IsInstanceOfType<BadRequestObjectResult>(await _controller.Journal(null, movementTypes: "Shipment,999"));
        _mediatorMock.Verify(x => x.Send(It.IsAny<GetStockJournalQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
