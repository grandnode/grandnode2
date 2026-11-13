using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class MerchandiseReturnWorkflowServiceTests
{
    private Mock<IInventoryDocumentService> _inventoryDocumentService;
    private Mock<IMerchandiseReturnLabelService> _labelService;
    private Mock<IMediator> _mediator;
    private Mock<IMerchandiseReturnMessageProviderService> _messageProvider;
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;
    private Order _order;
    private Mock<IOrderService> _orderService;
    private Mock<IResourceLock> _resourceLock;
    private Mock<IReturnShippingService> _returnShippingService;
    private MerchandiseReturnWorkflowService _service;
    private MerchandiseReturn _stored;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1", CustomerLanguageId = "customer-language" };
        _order.OrderItems.Add(new OrderItem { Id = "a", Quantity = 5 });
        _stored = Return(MerchandiseReturnStatus.Pending);

        _merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(() => _stored);
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons(It.IsAny<string>())).ReturnsAsync([]);
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions(It.IsAny<string>())).ReturnsAsync([]);
        _orderService = new Mock<IOrderService>();
        _orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        _mediator = new Mock<IMediator>();
        SetupReturnable(5);
        _resourceLock = new Mock<IResourceLock>();
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _inventoryDocumentService = new Mock<IInventoryDocumentService>();
        SetupReceived(false);
        _messageProvider = new Mock<IMerchandiseReturnMessageProviderService>();
        //no return method in the store: the return is sent back as it always was
        _returnShippingService = new Mock<IReturnShippingService>();
        SetupResolution(new ReturnShippingResolution());
        _labelService = new Mock<IMerchandiseReturnLabelService>();

        _service = new MerchandiseReturnWorkflowService(_merchandiseReturnService.Object, _orderService.Object,
            _mediator.Object, _resourceLock.Object, _inventoryDocumentService.Object, _messageProvider.Object,
            _returnShippingService.Object, _labelService.Object, NullLogger<MerchandiseReturnWorkflowService>.Instance);
    }

    private static MerchandiseReturn Return(MerchandiseReturnStatus status)
    {
        var merchandiseReturn = new MerchandiseReturn
            { Id = "r1", OrderId = "o1", MerchandiseReturnStatus = status };
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = 2 });
        return merchandiseReturn;
    }

    private void SetupReturnable(int quantity)
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { { "a", quantity } });
    }

    private void SetupReceived(bool received)
    {
        _inventoryDocumentService.Setup(x => x.IsReturnReceived("r1")).ReturnsAsync(received);
    }

    private void SetupResolution(ReturnShippingResolution resolution)
    {
        _returnShippingService.Setup(x =>
                x.Resolve(It.IsAny<MerchandiseReturnSubmitRequest>(), It.IsAny<ReturnShippingChoice>()))
            .ReturnsAsync(resolution);
    }

    private void SetupShipping(string optionCode)
    {
        SetupResolution(new ReturnShippingResolution {
            ByVendor = new Dictionary<string, MerchandiseReturnShipping> { { "", Shipping(optionCode) } }
        });
    }

    private static MerchandiseReturnShipping Shipping(string optionCode)
    {
        return new MerchandiseReturnShipping {
            ProviderSystemName = "Carrier", OptionCode = optionCode, OptionName = "Parcel", Cost = 5,
            LabelStatus = ReturnLabelStatus.Pending,
            ProviderData = new Dictionary<string, string> { { "k", optionCode } }
        };
    }

    private List<MerchandiseReturn> RecordInserts()
    {
        var inserted = new List<MerchandiseReturn>();
        _merchandiseReturnService.Setup(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback<MerchandiseReturn>(inserted.Add).Returns(Task.CompletedTask);
        return inserted;
    }

    private List<string> RecordLockRelease()
    {
        var steps = new List<string>();
        var held = new Mock<IAsyncDisposable>();
        held.Setup(x => x.DisposeAsync()).Callback(() => steps.Add("release")).Returns(ValueTask.CompletedTask);
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(held.Object);
        return steps;
    }

    [TestMethod]
    public async Task ChangeStatus_AllowedTransition_SavesStatusAndSendsCustomerMessageInOrderLanguage()
    {
        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.IsNull(error);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.Is<MerchandiseReturn>(r =>
            r.MerchandiseReturnStatus == MerchandiseReturnStatus.Accepted)), Times.Once);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            It.Is<MerchandiseReturn>(r => r.Id == "r1"), _order, "customer-language"), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_NotInTable_ReturnsStatusChangeNotAllowed_AndSavesNothing()
    {
        _stored = Return(MerchandiseReturnStatus.Completed);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Pending);

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, error);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            It.IsAny<MerchandiseReturn>(), It.IsAny<Order>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_ReadsStoredStatus_NotTheCallersCopy()
    {
        //the page was loaded while the return was pending; it has been rejected since
        _stored = Return(MerchandiseReturnStatus.Rejected);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Received);

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, error);
    }

    [TestMethod]
    public async Task ChangeStatus_UnknownReturn_ReturnsNotFound()
    {
        _stored = null;

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, error);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    public async Task ChangeStatus_RejectWithReceivedGoods_ReturnsAlreadyReceived(MerchandiseReturnStatus status)
    {
        _stored = Return(MerchandiseReturnStatus.Accepted);
        SetupReceived(true);

        var error = await _service.ChangeStatus("r1", status);

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyReceived, error);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_ReopenWithoutQuantityLeft_ReturnsQuantityExceeded()
    {
        //another return took the quantity this one gave back when it was rejected
        _stored = Return(MerchandiseReturnStatus.Rejected);
        SetupReturnable(1);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Pending);

        Assert.AreEqual(MerchandiseReturnErrors.QuantityExceeded, error);
        _mediator.Verify(x => x.Send(It.Is<GetReturnableQuantitiesQuery>(q => q.ForStaff && q.Order == _order),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_ReopenWithQuantityLeft_SavesPending()
    {
        _stored = Return(MerchandiseReturnStatus.Cancelled);
        SetupReturnable(2);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Pending);

        Assert.IsNull(error);
        Assert.AreEqual(MerchandiseReturnStatus.Pending, _stored.MerchandiseReturnStatus);
    }

    [TestMethod]
    public async Task ChangeStatus_RejectWithNote_InsertsCustomerVisibleNote_AndNoNoteAddedMessage()
    {
        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "Used item");

        Assert.IsNull(error);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.MerchandiseReturnId == "r1" && n.Note == "Used item" && n.DisplayToCustomer &&
            !n.CreatedByCustomer)), Times.Once);
        _messageProvider.Verify(x => x.SendNewMerchandiseReturnNoteAddedCustomerMessage(
            It.IsAny<MerchandiseReturn>(), It.IsAny<MerchandiseReturnNote>(), It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_BlankNote_InsertsNoNote()
    {
        await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "  ");

        //only the staff note of the change: nothing for the customer
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.DisplayToCustomer)), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_Accept_InsertsStaffNoteNamingWhoChangedIt()
    {
        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, changedBy: "john@shop.com");

        Assert.IsNull(error);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.MerchandiseReturnId == "r1" && !n.DisplayToCustomer && !n.CreatedByCustomer &&
            n.Note == "Merchandise return status has been changed to Accepted by john@shop.com")), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_NoActor_StaffNoteNamesSystem()
    {
        await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            !n.DisplayToCustomer && n.Note == "Merchandise return status has been changed to Accepted by system")),
            Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_StaffNote_WrittenUnderTheLockAfterTheStatus()
    {
        var steps = RecordLockRelease();
        _merchandiseReturnService.Setup(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback(() => steps.Add("status")).Returns(Task.CompletedTask);
        _merchandiseReturnService.Setup(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()))
            .Callback(() => steps.Add("note")).Returns(Task.CompletedTask);

        await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, changedBy: "john@shop.com");

        CollectionAssert.AreEqual(new[] { "status", "note", "release" }, steps);
    }

    [TestMethod]
    public async Task ChangeStatus_Refused_InsertsNoNote()
    {
        _stored = Return(MerchandiseReturnStatus.Completed);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Pending, changedBy: "john@shop.com");

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, error);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()),
            Times.Never);
    }

    [TestMethod]
    public async Task MarkReceived_StaffNoteNamesThePoster()
    {
        _stored = Return(MerchandiseReturnStatus.Accepted);

        await _service.MarkReceived("r1", "poster@shop.com");

        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            !n.DisplayToCustomer &&
            n.Note == "Merchandise return status has been changed to Received by poster@shop.com")), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_HoldsTheOrdersQuantityLock()
    {
        await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        _resourceLock.Verify(x => x.Acquire(
            It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_LockTimeout_ReturnsBusy()
    {
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceLockTimeoutException("MerchandiseReturn:o1"));

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.AreEqual(MerchandiseReturnErrors.Busy, error);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Update_DoesNotOverwriteStatusChangedMeanwhile()
    {
        //the form was loaded while the return was pending; it has been accepted since
        _stored = Return(MerchandiseReturnStatus.Accepted);

        var error = await _service.Update("r1", x =>
        {
            x.MerchandiseReturnStatus = MerchandiseReturnStatus.Pending;
            x.StaffNotes = "x";
        });

        Assert.IsNull(error);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.Is<MerchandiseReturn>(r =>
            r.MerchandiseReturnStatus == MerchandiseReturnStatus.Accepted && r.StaffNotes == "x")), Times.Once);
    }

    [TestMethod]
    public async Task Update_UnknownReturn_ReturnsNotFound()
    {
        _stored = null;

        Assert.AreEqual(MerchandiseReturnErrors.NotFound, await _service.Update("r1", _ => { }));
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Pending)]
    [DataRow(MerchandiseReturnStatus.Accepted)]
    public async Task MarkReceived_PendingOrAccepted_ChangesToReceived(MerchandiseReturnStatus status)
    {
        _stored = Return(status);

        await _service.MarkReceived("r1");

        Assert.AreEqual(MerchandiseReturnStatus.Received, _stored.MerchandiseReturnStatus);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            _stored, _order, "customer-language"), Times.Once);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Received)]
    [DataRow(MerchandiseReturnStatus.Completed)]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    public async Task MarkReceived_OtherStatus_DoesNothing(MerchandiseReturnStatus status)
    {
        //a second receipt of the same return, or a return closed already
        _stored = Return(status);

        await _service.MarkReceived("r1");

        Assert.AreEqual(status, _stored.MerchandiseReturnStatus);
        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task MarkReceived_LockTimeout_DoesNotThrow_AndLeavesTheReturn()
    {
        //the receipt is posted already: a refusal is logged, not reported as a failed posting
        _stored = Return(MerchandiseReturnStatus.Accepted);
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceLockTimeoutException("MerchandiseReturn:o1"));

        await _service.MarkReceived("r1");

        Assert.AreEqual(MerchandiseReturnStatus.Accepted, _stored.MerchandiseReturnStatus);
    }

    [TestMethod]
    public async Task ChangeStatus_MessageFails_StatusStaysSaved_AndNoErrorReturned()
    {
        _messageProvider.Setup(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
                It.IsAny<MerchandiseReturn>(), It.IsAny<Order>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp"));

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.IsNull(error);
        Assert.AreEqual(MerchandiseReturnStatus.Accepted, _stored.MerchandiseReturnStatus);
    }

    [TestMethod]
    public async Task ChangeStatus_OrderMissing_SavesStatus_AndSendsNoMessage()
    {
        _orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync((Order)null);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.IsNull(error);
        Assert.AreEqual(MerchandiseReturnStatus.Accepted, _stored.MerchandiseReturnStatus);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            It.IsAny<MerchandiseReturn>(), It.IsAny<Order>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task MarkReceived_UnknownReturn_DoesNothing()
    {
        _stored = null;

        await _service.MarkReceived("r1");

        _merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    private MerchandiseReturnSubmitRequest SubmitRequest(params MerchandiseReturnSubmitItem[] items)
    {
        return new MerchandiseReturnSubmitRequest {
            Order = _order, StoreId = "s1", CustomerId = "c1", OwnerId = "owner", LanguageId = "l1",
            Comments = "comments", Items = items.ToList()
        };
    }

    private void TwoVendorOrder()
    {
        _order.OrderItems.Clear();
        _order.OrderItems.Add(new OrderItem { Id = "store-item", Quantity = 2 });
        _order.OrderItems.Add(new OrderItem { Id = "vendor-item", VendorId = "v1", Quantity = 2 });
        _order.OrderItems.Add(new OrderItem { Id = "store-item-2", VendorId = "", Quantity = 2 });
        _mediator.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { { "store-item", 2 }, { "vendor-item", 2 }, { "store-item-2", 2 } });
    }

    [TestMethod]
    public async Task Submit_ItemsOfTwoVendors_CreatesOneReturnPerVendor()
    {
        TwoVendorOrder();

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("store-item", 1, null, null),
            new MerchandiseReturnSubmitItem("vendor-item", 2, null, null)));

        Assert.IsNull(result.Error);
        Assert.AreEqual(2, result.MerchandiseReturns.Count);
        var vendorReturn = result.MerchandiseReturns.Single(x => x.VendorId == "v1");
        Assert.AreEqual("vendor-item", vendorReturn.MerchandiseReturnItems.Single().OrderItemId);
        Assert.AreEqual(2, vendorReturn.MerchandiseReturnItems.Single().Quantity);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Exactly(2));
    }

    [TestMethod]
    public async Task Submit_StoreAndVendorItems_OneReturnEach()
    {
        //a null and an empty vendor are both the store's own items
        TwoVendorOrder();

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("store-item", 1, null, null),
            new MerchandiseReturnSubmitItem("store-item-2", 1, null, null),
            new MerchandiseReturnSubmitItem("vendor-item", 1, null, null)));

        var storeReturn = result.MerchandiseReturns.Single(x => x.VendorId == "");
        CollectionAssert.AreEquivalent(new[] { "store-item", "store-item-2" },
            storeReturn.MerchandiseReturnItems.Select(x => x.OrderItemId).ToArray());
        Assert.AreEqual(2, result.MerchandiseReturns.Count);
    }

    [TestMethod]
    public async Task Submit_FillsTheReturnFromTheRequest()
    {
        _order.SeId = "se1";

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 2, null, null)));

        var merchandiseReturn = result.MerchandiseReturns.Single();
        Assert.AreEqual("s1", merchandiseReturn.StoreId);
        Assert.AreEqual("o1", merchandiseReturn.OrderId);
        Assert.AreEqual("c1", merchandiseReturn.CustomerId);
        Assert.AreEqual("owner", merchandiseReturn.OwnerId);
        Assert.AreEqual("se1", merchandiseReturn.SeId);
        Assert.AreEqual("comments", merchandiseReturn.CustomerComments);
        Assert.AreEqual(MerchandiseReturnStatus.Pending, merchandiseReturn.MerchandiseReturnStatus);
    }

    [TestMethod]
    public async Task Submit_StoresReasonIdAndActionId_AndTheNamesInTheRequestLanguage()
    {
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons("s1"))
            .ReturnsAsync([
                new MerchandiseReturnReason {
                    Id = "rr1", Name = "Broken",
                    Locales = [new TranslationEntity { LanguageId = "l1", LocaleKey = "Name", LocaleValue = "Zepsuty" }]
                }
            ]);
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions("s1"))
            .ReturnsAsync([new MerchandiseReturnAction { Id = "ra1", Name = "Refund" }]);

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, "rr1", "ra1")));

        //the Ids say what was chosen, the names what the customer read
        var item = result.MerchandiseReturns.Single().MerchandiseReturnItems.Single();
        Assert.AreEqual("rr1", item.ReasonId);
        Assert.AreEqual("Zepsuty", item.ReasonForReturn);
        Assert.AreEqual("ra1", item.ActionId);
        Assert.AreEqual("Refund", item.RequestedAction);
    }

    [TestMethod]
    public async Task Submit_ReasonOrActionTheStoreDoesNotOffer_CountsAsNoneChosen()
    {
        //the ids of another store's reason and action, posted by hand: the store offers neither
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnReasons("s1")).ReturnsAsync([]);
        _merchandiseReturnService.Setup(x => x.GetAllMerchandiseReturnActions("s1")).ReturnsAsync([]);
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnReasonById("other-store-reason"))
            .ReturnsAsync(new MerchandiseReturnReason { Id = "other-store-reason", Name = "Secret" });

        var result = await _service.Submit(SubmitRequest(
            new MerchandiseReturnSubmitItem("a", 1, "other-store-reason", "other-store-action")));

        var item = result.MerchandiseReturns.Single().MerchandiseReturnItems.Single();
        Assert.IsNull(item.ReasonId);
        Assert.AreEqual("not available", item.ReasonForReturn);
        Assert.IsNull(item.ActionId);
        //no action asked for: nothing to show
        Assert.AreEqual("", item.RequestedAction);
    }

    [TestMethod]
    public async Task Submit_QuantityAboveReturnable_CreatesNothing_ReturnsSubmitQuantityExceeded()
    {
        //another return took 3 of the 5 in the meantime: not a smaller return the customer did not ask for
        SetupReturnable(2);

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 5, null, null)));

        Assert.AreEqual(MerchandiseReturnErrors.SubmitQuantityExceeded, result.Error);
        Assert.AreEqual(0, result.MerchandiseReturns.Count);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Submit_ItemNotInOrder_ReturnsSubmitQuantityExceeded()
    {
        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("other", 1, null, null)));

        Assert.AreEqual(MerchandiseReturnErrors.SubmitQuantityExceeded, result.Error);
    }

    [TestMethod]
    public async Task Submit_NoPositiveQuantity_ReturnsSubmitNothingToReturn()
    {
        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 0, null, null)));

        Assert.AreEqual(MerchandiseReturnErrors.SubmitNothingToReturn, result.Error);
        _resourceLock.Verify(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Submit_LockTimeout_ReturnsSubmitBusy()
    {
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceLockTimeoutException("MerchandiseReturn:o1"));

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        Assert.AreEqual(MerchandiseReturnErrors.SubmitBusy, result.Error);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Submit_ComputesReturnableOnce_WhileHoldingTheOrderLock()
    {
        //record when the lock is taken and let go, against the check and the inserts
        TwoVendorOrder();
        var steps = new List<string>();
        var held = new Mock<IAsyncDisposable>();
        held.Setup(x => x.DisposeAsync()).Callback(() => steps.Add("release")).Returns(ValueTask.CompletedTask);
        _resourceLock.Setup(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"),
                It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("acquire")).ReturnsAsync(held.Object);
        _mediator.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("check"))
            .ReturnsAsync(new Dictionary<string, int> { { "store-item", 2 }, { "vendor-item", 2 } });
        _merchandiseReturnService.Setup(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback(() => steps.Add("insert")).Returns(Task.CompletedTask);

        await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("store-item", 1, null, null),
            new MerchandiseReturnSubmitItem("vendor-item", 1, null, null)));

        CollectionAssert.AreEqual(new[] { "acquire", "check", "insert", "insert", "release" }, steps);
    }

    [TestMethod]
    public async Task Submit_SecondInsertFails_DeletesTheFirst_AndThrows()
    {
        //no return is left saved that no message told anyone about
        TwoVendorOrder();
        var inserts = 0;
        _merchandiseReturnService.Setup(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Returns(() => ++inserts == 2 ? Task.FromException(new TimeoutException()) : Task.CompletedTask);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => _service.Submit(SubmitRequest(
            new MerchandiseReturnSubmitItem("store-item", 1, null, null),
            new MerchandiseReturnSubmitItem("vendor-item", 1, null, null))));

        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task Delete_WithReceivedGoods_ReturnsAlreadyReceived()
    {
        //deleting gives the quantity back to the customer, like rejecting does: not once the goods are in stock
        _stored = Return(MerchandiseReturnStatus.Accepted);
        SetupReceived(true);

        var error = await _service.Delete("r1");

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyReceived, error);
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Received)]
    [DataRow(MerchandiseReturnStatus.Completed)]
    public async Task Delete_ReceivedOrCompleted_ReturnsAlreadyReceived(MerchandiseReturnStatus status)
    {
        //marked received by hand, without a receipt: the goods are back all the same
        _stored = Return(status);

        var error = await _service.Delete("r1");

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyReceived, error);
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HoldsQuantityLock_AndDeletes()
    {
        var error = await _service.Delete("r1");

        Assert.IsNull(error);
        _resourceLock.Verify(x => x.Acquire(
            It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"), It.IsAny<CancellationToken>()),
            Times.Once);
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(_stored), Times.Once);
    }

    [TestMethod]
    public async Task Submit_WithChoice_SetsSnapshotPerVendor()
    {
        TwoVendorOrder();
        var storeShipping = Shipping("store-option");
        var vendorShipping = Shipping("vendor-option");
        SetupResolution(new ReturnShippingResolution {
            ByVendor = new Dictionary<string, MerchandiseReturnShipping>
                { { "", storeShipping }, { "v1", vendorShipping } }
        });
        var inserted = RecordInserts();
        var request = SubmitRequest(new MerchandiseReturnSubmitItem("store-item", 1, null, null),
            new MerchandiseReturnSubmitItem("vendor-item", 1, null, null));
        request.ReturnShipping = new ReturnShippingChoice { ProviderSystemName = "Carrier", OptionCode = "parcel" };

        var result = await _service.Submit(request);

        Assert.IsNull(result.Error);
        Assert.AreEqual(2, result.MerchandiseReturns.Count);
        _returnShippingService.Verify(x => x.Resolve(request, request.ReturnShipping), Times.Once);
        //each return keeps a copy of its own: a change of one return's shipping is no change of another's
        var storeReturn = inserted.Single(x => x.VendorId == "");
        var vendorReturn = inserted.Single(x => x.VendorId == "v1");
        Assert.AreEqual("store-option", storeReturn.ReturnShipping.OptionCode);
        Assert.AreEqual("vendor-option", vendorReturn.ReturnShipping.OptionCode);
        Assert.AreEqual("Carrier", vendorReturn.ReturnShipping.ProviderSystemName);
        Assert.AreEqual(ReturnLabelStatus.Pending, vendorReturn.ReturnShipping.LabelStatus);
        Assert.AreEqual(5, vendorReturn.ReturnShipping.Cost);
        Assert.AreNotSame(storeShipping, storeReturn.ReturnShipping);
        Assert.AreNotSame(vendorShipping, vendorReturn.ReturnShipping);
        Assert.AreNotSame(vendorShipping.ProviderData, vendorReturn.ReturnShipping.ProviderData);
        Assert.AreEqual("vendor-option", vendorReturn.ReturnShipping.ProviderData["k"]);
        _labelService.Verify(x => x.CreateLabel(storeReturn.Id), Times.Once);
        _labelService.Verify(x => x.CreateLabel(vendorReturn.Id), Times.Once);
    }

    [TestMethod]
    public async Task Submit_ShippingErrors_NothingSaved()
    {
        SetupResolution(new ReturnShippingResolution
            { Errors = [MerchandiseReturnErrors.SubmitReturnShippingChoose, "Pick a point"] });

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        //passed on as they are: a key is translated by the storefront, a provider's text is translated already
        CollectionAssert.AreEqual(new[] { MerchandiseReturnErrors.SubmitReturnShippingChoose, "Pick a point" },
            result.ShippingErrors.ToArray());
        Assert.AreEqual(0, result.MerchandiseReturns.Count);
        _resourceLock.Verify(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
        _labelService.Verify(x => x.CreateLabel(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Submit_LabelCreatedAfterLockReleased()
    {
        //the label service takes the order's lock to save the label: called under it, it would wait for itself
        SetupShipping("parcel");
        var steps = RecordLockRelease();
        _merchandiseReturnService.Setup(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback(() => steps.Add("insert")).Returns(Task.CompletedTask);
        _labelService.Setup(x => x.CreateLabel(It.IsAny<string>())).Callback(() => steps.Add("label"))
            .ReturnsAsync((string)null);

        await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        CollectionAssert.AreEqual(new[] { "insert", "release", "label" }, steps);
    }

    [TestMethod]
    public async Task Submit_LabelFails_ReturnsStillCreated()
    {
        //the return is saved and its messages are sent; the label is created again later
        SetupShipping("parcel");
        _labelService.Setup(x => x.CreateLabel(It.IsAny<string>())).ThrowsAsync(new TimeoutException());

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        Assert.IsNull(result.Error);
        Assert.AreEqual(0, result.ShippingErrors.Count);
        Assert.AreEqual("parcel", result.MerchandiseReturns.Single().ReturnShipping.OptionCode);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Once);
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Submit_LabelCreated_ResultHoldsTheReturnAsSavedWithItsLabel()
    {
        //the messages the caller sends show the label the label service saved
        SetupShipping("parcel");
        var inserted = RecordInserts();
        var saved = new MerchandiseReturn {
            ReturnShipping = new MerchandiseReturnShipping
                { OptionCode = "parcel", LabelStatus = ReturnLabelStatus.Created, TrackingNumber = "T1" }
        };
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById(It.Is<string>(id => id != "r1")))
            .ReturnsAsync(saved);

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        Assert.AreSame(saved, result.MerchandiseReturns.Single());
        _merchandiseReturnService.Verify(x => x.GetMerchandiseReturnById(inserted.Single().Id), Times.Once);
    }

    [TestMethod]
    public async Task Submit_NoProviders_UnchangedFlow()
    {
        var inserted = RecordInserts();

        var result = await _service.Submit(SubmitRequest(new MerchandiseReturnSubmitItem("a", 1, null, null)));

        Assert.IsNull(result.Error);
        Assert.AreEqual(0, result.ShippingErrors.Count);
        Assert.IsNull(inserted.Single().ReturnShipping);
        Assert.AreSame(inserted.Single(), result.MerchandiseReturns.Single());
        _labelService.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    public async Task ChangeStatus_Reject_CancelsCreatedLabel(MerchandiseReturnStatus status)
    {
        //cancelled with the carrier once the order's lock is let go, before the customer is told
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };
        var steps = RecordLockRelease();
        _labelService.Setup(x => x.CancelLabel("r1")).Callback(() => steps.Add("cancel")).Returns(Task.CompletedTask);
        _messageProvider.Setup(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
                It.IsAny<MerchandiseReturn>(), It.IsAny<Order>(), It.IsAny<string>()))
            .Callback(() => steps.Add("message")).ReturnsAsync(1);

        var error = await _service.ChangeStatus("r1", status);

        Assert.IsNull(error);
        CollectionAssert.AreEqual(new[] { "release", "cancel", "message" }, steps);
    }

    [TestMethod]
    public async Task ChangeStatus_Reject_MessageSentWithTheReturnAsCancelled()
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };
        var afterCancel = Return(MerchandiseReturnStatus.Rejected);
        afterCancel.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Cancelled };
        //the label service saves the cancelled label: the return is read again as stored then
        _labelService.Setup(x => x.CancelLabel("r1")).Callback(() => _stored = afterCancel)
            .Returns(Task.CompletedTask);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected);

        Assert.IsNull(error);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            It.Is<MerchandiseReturn>(r => r.ReturnShipping.LabelStatus == ReturnLabelStatus.Cancelled), _order,
            "customer-language"), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_Reject_ReadAgainFails_MessageSentWithTheReturnInMemory()
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };
        var inMemory = _stored;
        _labelService.Setup(x => x.CancelLabel("r1")).Callback(() =>
                _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById("r1"))
                    .ThrowsAsync(new TimeoutException()))
            .Returns(Task.CompletedTask);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected);

        Assert.IsNull(error);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            inMemory, _order, "customer-language"), Times.Once);
    }

    [TestMethod]
    public async Task ChangeStatus_Reject_CancelFails_StatusStaysChanged_AndMessageSent()
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };
        _labelService.Setup(x => x.CancelLabel("r1")).ThrowsAsync(new TimeoutException());

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected);

        Assert.IsNull(error);
        Assert.AreEqual(MerchandiseReturnStatus.Rejected, _stored.MerchandiseReturnStatus);
        _messageProvider.Verify(x => x.SendMerchandiseReturnStatusChangedCustomerMessage(
            _stored, _order, "customer-language"), Times.Once);
    }

    [TestMethod]
    [DataRow(ReturnLabelStatus.NotRequired)]
    [DataRow(ReturnLabelStatus.Pending)]
    [DataRow(ReturnLabelStatus.Failed)]
    public async Task ChangeStatus_Reject_NoCreatedLabel_DoesNotCancel(ReturnLabelStatus labelStatus)
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = labelStatus };

        await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected);

        _labelService.Verify(x => x.CancelLabel(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_Accept_DoesNotCancel()
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Accepted);

        Assert.IsNull(error);
        _labelService.Verify(x => x.CancelLabel(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeStatus_RejectRefused_DoesNotCancel()
    {
        //the goods are back already: the return stays as it is, and so does its label
        _stored = Return(MerchandiseReturnStatus.Accepted);
        _stored.ReturnShipping = new MerchandiseReturnShipping { LabelStatus = ReturnLabelStatus.Created };
        SetupReceived(true);

        var error = await _service.ChangeStatus("r1", MerchandiseReturnStatus.Rejected);

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyReceived, error);
        _labelService.Verify(x => x.CancelLabel(It.IsAny<string>()), Times.Never);
    }
}
