using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Module.Api.Commands.Handlers.Orders;
using Grand.Module.Api.Commands.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Orders;

[TestClass]
public class ChangeMerchandiseReturnStatusCommandHandlerTests
{
    private Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;
    private Mock<IMerchandiseReturnWorkflowService> _workflowServiceMock;
    private readonly MerchandiseReturn _stored = new() { Id = "r1", MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted };

    [TestInitialize]
    public void Init()
    {
        _workflowServiceMock = new Mock<IMerchandiseReturnWorkflowService>();
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnServiceMock.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(_stored);
    }

    private ChangeMerchandiseReturnStatusCommandHandler CreateHandler() =>
        new(_workflowServiceMock.Object, _merchandiseReturnServiceMock.Object);

    [TestMethod]
    public async Task Handle_ReceivedWhereAReceiptTakesTheGoodsIn_RefusedWithoutChange()
    {
        _workflowServiceMock.Setup(w => w.CanMarkReceived(_stored)).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new ChangeMerchandiseReturnStatusCommand {
            Id = "r1", Status = (int)MerchandiseReturnStatus.Received
        }, default);

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, result);
        _workflowServiceMock.Verify(w => w.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_ReceivedAllowedByHand_Changed()
    {
        _workflowServiceMock.Setup(w => w.CanMarkReceived(_stored)).ReturnsAsync(true);

        var result = await CreateHandler().Handle(new ChangeMerchandiseReturnStatusCommand {
            Id = "r1", Status = (int)MerchandiseReturnStatus.Received, ChangedBy = "api@x.com"
        }, default);

        Assert.IsNull(result);
        _workflowServiceMock.Verify(w => w.ChangeStatus("r1", MerchandiseReturnStatus.Received, null, "api@x.com"),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_ReceivedOfUnknownReturn_NotFound()
    {
        var result = await CreateHandler().Handle(new ChangeMerchandiseReturnStatusCommand {
            Id = "missing", Status = (int)MerchandiseReturnStatus.Received
        }, default);

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, result);
    }

    [TestMethod]
    public async Task Handle_ForwardsStatusNoteAndChangedBy()
    {
        _workflowServiceMock.Setup(w =>
                w.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, "note", "staff@x.com"))
            .ReturnsAsync((string)null);

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
