using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Module.Api.Commands.Handlers.Orders;
using Grand.Module.Api.Commands.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Orders;

[TestClass]
public class ChangeMerchandiseReturnStatusCommandHandlerTests
{
    private Mock<IMerchandiseReturnWorkflowService> _workflowServiceMock;

    public ChangeMerchandiseReturnStatusCommandHandlerTests()
    {
        _workflowServiceMock = new Mock<IMerchandiseReturnWorkflowService>();
    }

    private ChangeMerchandiseReturnStatusCommandHandler CreateHandler() => new(_workflowServiceMock.Object);

    [TestMethod]
    public async Task Handle_ForwardsStatusNoteAndChangedBy()
    {
        _workflowServiceMock.Setup(w =>
                w.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, "note", "staff@x.com"))
            .ReturnsAsync((string?)null);

        var result = await CreateHandler().Handle(new ChangeMerchandiseReturnStatusCommand {
            Id = "r1", Status = (int)MerchandiseReturnStatus.Accepted, CustomerNote = "note", ChangedBy = "staff@x.com"
        }, default);

        Assert.IsNull(result);
        _workflowServiceMock.Verify(w => w.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, "note", "staff@x.com"),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReturnsErrorKey()
    {
        _workflowServiceMock.Setup(w => w.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Orders.MerchandiseReturns.StatusChangeNotAllowed");

        var result = await CreateHandler().Handle(
            new ChangeMerchandiseReturnStatusCommand { Id = "r1", Status = 40 }, default);

        Assert.AreEqual("Orders.MerchandiseReturns.StatusChangeNotAllowed", result);
    }
}
