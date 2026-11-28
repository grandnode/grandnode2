using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Module.Api.Commands.Handlers.Orders;
using Grand.Module.Api.Commands.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Orders;

[TestClass]
public class DeleteMerchandiseReturnCommandHandlerTests
{
    private Mock<IMerchandiseReturnWorkflowService> _workflowServiceMock;

    public DeleteMerchandiseReturnCommandHandlerTests()
    {
        _workflowServiceMock = new Mock<IMerchandiseReturnWorkflowService>();
    }

    [TestMethod]
    public async Task Handle_ForwardsId_AndReturnsErrorKey()
    {
        _workflowServiceMock.Setup(w => w.Delete("r1"))
            .ReturnsAsync("Orders.MerchandiseReturns.AlreadyReceived");

        var result = await new DeleteMerchandiseReturnCommandHandler(_workflowServiceMock.Object)
            .Handle(new DeleteMerchandiseReturnCommand { Id = "r1" }, default);

        Assert.AreEqual("Orders.MerchandiseReturns.AlreadyReceived", result);
        _workflowServiceMock.Verify(w => w.Delete("r1"), Times.Once);
    }
}
