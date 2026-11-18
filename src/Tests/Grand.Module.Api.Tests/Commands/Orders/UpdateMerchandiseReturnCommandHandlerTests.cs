using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Module.Api.Commands.Handlers.Orders;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Domain.Common;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.DTOs.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Orders;

[TestClass]
public class UpdateMerchandiseReturnCommandHandlerTests
{
    private static readonly DateTime StoredPickupDate = new(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc);

    private Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;
    private Mock<ISettingService> _settingServiceMock;
    private MerchandiseReturn _stored;
    private Mock<IMerchandiseReturnWorkflowService> _workflowServiceMock;

    [TestInitialize]
    public void Init()
    {
        _workflowServiceMock = new Mock<IMerchandiseReturnWorkflowService>();
        _stored = new MerchandiseReturn {
            Id = "r1", StoreId = "s2", PickupDate = StoredPickupDate, PickupAddress = new Address { City = "Gdansk" }
        };
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnServiceMock.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(() => _stored);
        _settingServiceMock = new Mock<ISettingService>();
        SetupPickupAllowed(true);
    }

    //the settings of the return's store, never of the store the API request came through
    private void SetupPickupAllowed(bool allowed)
    {
        _settingServiceMock.Setup(x => x.LoadSetting<OrderSettings>("s2")).ReturnsAsync(new OrderSettings {
            MerchandiseReturns_AllowToSpecifyPickupDate = allowed,
            MerchandiseReturns_AllowToSpecifyPickupAddress = allowed
        });
    }

    private UpdateMerchandiseReturnCommandHandler CreateHandler() =>
        new(_workflowServiceMock.Object, _merchandiseReturnServiceMock.Object, _settingServiceMock.Object);

    private Action<MerchandiseReturn> CaptureApply()
    {
        Action<MerchandiseReturn> apply = null;
        _workflowServiceMock.Setup(w => w.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null)
            .Callback<string, Action<MerchandiseReturn>>((_, a) => apply = a);
        return stored => apply(stored);
    }

    private Task<string> Handle(MerchandiseReturnDto model)
    {
        return CreateHandler().Handle(new UpdateMerchandiseReturnCommand { Id = "r1", Model = model }, default);
    }

    [TestMethod]
    public async Task Handle_OnlyStaffNotesSent_KeepsTheOtherFields()
    {
        var apply = CaptureApply();
        var pickupAddress = _stored.PickupAddress;

        await Handle(new MerchandiseReturnDto { StaffNotes = "notes" });

        var stored = new MerchandiseReturn {
            CustomerComments = "customer", ExternalId = "ext", PickupDate = StoredPickupDate,
            PickupAddress = pickupAddress
        };
        apply(stored);

        Assert.AreEqual("notes", stored.StaffNotes);
        Assert.AreEqual("customer", stored.CustomerComments);
        Assert.AreEqual("ext", stored.ExternalId);
        Assert.AreEqual(StoredPickupDate, stored.PickupDate);
        Assert.AreSame(pickupAddress, stored.PickupAddress);
        //nothing of the pickup sent: the store's settings are not asked
        _settingServiceMock.Verify(x => x.LoadSetting<OrderSettings>(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_PickupDateTheStoreDoesNotAllow_RefusedAndNothingSaved()
    {
        SetupPickupAllowed(false);

        var error = await Handle(new MerchandiseReturnDto {
            PickupDate = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.AreEqual(MerchandiseReturnErrors.PickupNotAllowed, error);
        _workflowServiceMock.Verify(w => w.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_PickupAddressTheStoreDoesNotAllow_RefusedAndNothingSaved()
    {
        SetupPickupAllowed(false);

        var error = await Handle(new MerchandiseReturnDto { PickupAddress = new AddressDto { City = "Krakow" } });

        Assert.AreEqual(MerchandiseReturnErrors.PickupNotAllowed, error);
        _workflowServiceMock.Verify(w => w.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_PickupNotAllowed_SentBackAsStored_SavesTheRestAndKeepsThePickup()
    {
        //a client that reads the return and puts it back with its notes changed
        SetupPickupAllowed(false);
        var apply = CaptureApply();

        var error = await Handle(new MerchandiseReturnDto {
            StaffNotes = "notes", PickupDate = StoredPickupDate, PickupAddress = new AddressDto { City = "Gdansk" }
        });

        Assert.IsNull(error);
        var stored = new MerchandiseReturn { PickupDate = StoredPickupDate, PickupAddress = _stored.PickupAddress };
        apply(stored);
        Assert.AreEqual("notes", stored.StaffNotes);
        Assert.AreSame(_stored.PickupAddress, stored.PickupAddress);
    }

    [TestMethod]
    public async Task Handle_EmptyPickupAddress_ClearsIt()
    {
        var apply = CaptureApply();

        await Handle(new MerchandiseReturnDto { PickupAddress = new AddressDto() });

        var stored = new MerchandiseReturn { PickupAddress = new Address { City = "Gdansk" } };
        apply(stored);
        Assert.IsNull(stored.PickupAddress);
    }

    [TestMethod]
    public async Task Handle_UnknownReturnWithPickup_NotFound()
    {
        _stored = null;

        var error = await Handle(new MerchandiseReturnDto { PickupDate = StoredPickupDate });

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, error);
    }

    [TestMethod]
    public async Task Handle_AppliesOnlyEditableFields()
    {
        var apply = CaptureApply();

        var pickup = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        await Handle(new MerchandiseReturnDto {
            CustomerComments = "comments", StaffNotes = "notes", ExternalId = "ext", PickupDate = pickup,
            MerchandiseReturnStatusId = (int)MerchandiseReturnStatus.Completed
        });

        var stored = new MerchandiseReturn { MerchandiseReturnStatusId = (int)MerchandiseReturnStatus.Pending };
        apply(stored);

        Assert.AreEqual("comments", stored.CustomerComments);
        Assert.AreEqual("notes", stored.StaffNotes);
        Assert.AreEqual("ext", stored.ExternalId);
        Assert.AreEqual(pickup, stored.PickupDate);
        //the status changes only through ChangeStatus, never through Update
        Assert.AreEqual((int)MerchandiseReturnStatus.Pending, stored.MerchandiseReturnStatusId);
    }

    [TestMethod]
    public async Task Handle_ReturnsErrorKey()
    {
        _workflowServiceMock.Setup(w => w.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync("Orders.MerchandiseReturns.NotFound");

        var result = await Handle(new MerchandiseReturnDto());

        Assert.AreEqual("Orders.MerchandiseReturns.NotFound", result);
    }

    [TestMethod]
    public async Task Handle_NullPickupDate_LeavesItUnchanged()
    {
        var apply = CaptureApply();

        await Handle(new MerchandiseReturnDto { CustomerComments = "comments", PickupDate = null });

        var stored = new MerchandiseReturn { PickupDate = StoredPickupDate };
        apply(stored);

        Assert.AreEqual(StoredPickupDate, stored.PickupDate);
    }
}
