using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Module.Api.Commands.Handlers.Orders;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Orders;

[TestClass]
public class UpdateMerchandiseReturnCommandHandlerTests
{
    private Mock<IMerchandiseReturnWorkflowService> _workflowServiceMock;

    [TestInitialize]
    public void Init()
    {
        _workflowServiceMock = new Mock<IMerchandiseReturnWorkflowService>();
    }

    private UpdateMerchandiseReturnCommandHandler CreateHandler() => new(_workflowServiceMock.Object);

    [TestMethod]
    public async Task Handle_AppliesOnlyEditableFields()
    {
        Action<MerchandiseReturn> apply = null;
        _workflowServiceMock.Setup(w => w.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null)
            .Callback<string, Action<MerchandiseReturn>>((_, a) => apply = a);

        var pickup = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        await CreateHandler().Handle(new UpdateMerchandiseReturnCommand {
            Id = "r1",
            Model = new MerchandiseReturnDto {
                CustomerComments = "comments", StaffNotes = "notes", ExternalId = "ext", PickupDate = pickup,
                PickupAddress = null, MerchandiseReturnStatusId = (int)MerchandiseReturnStatus.Completed
            }
        }, default);

        var stored = new MerchandiseReturn { MerchandiseReturnStatusId = (int)MerchandiseReturnStatus.Pending };
        apply(stored);

        Assert.AreEqual("comments", stored.CustomerComments);
        Assert.AreEqual("notes", stored.StaffNotes);
        Assert.AreEqual("ext", stored.ExternalId);
        Assert.AreEqual(pickup, stored.PickupDate);
        Assert.IsNull(stored.PickupAddress);
        //the status changes only through ChangeStatus, never through Update
        Assert.AreEqual((int)MerchandiseReturnStatus.Pending, stored.MerchandiseReturnStatusId);
    }

    [TestMethod]
    public async Task Handle_ReturnsErrorKey()
    {
        _workflowServiceMock.Setup(w => w.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync("Orders.MerchandiseReturns.NotFound");

        var result = await CreateHandler().Handle(
            new UpdateMerchandiseReturnCommand { Id = "r1", Model = new MerchandiseReturnDto() }, default);

        Assert.AreEqual("Orders.MerchandiseReturns.NotFound", result);
    }
}
