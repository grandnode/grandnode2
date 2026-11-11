using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
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
    private Mock<IMediator> _mediator;
    private Mock<IMerchandiseReturnMessageProviderService> _messageProvider;
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;
    private Order _order;
    private Mock<IResourceLock> _resourceLock;
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
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        _mediator = new Mock<IMediator>();
        SetupReturnable(5);
        _resourceLock = new Mock<IResourceLock>();
        _resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _inventoryDocumentService = new Mock<IInventoryDocumentService>();
        SetupPostedReceipts(0);
        _messageProvider = new Mock<IMerchandiseReturnMessageProviderService>();

        _service = new MerchandiseReturnWorkflowService(_merchandiseReturnService.Object, orderService.Object,
            _mediator.Object, _resourceLock.Object, _inventoryDocumentService.Object, _messageProvider.Object,
            NullLogger<MerchandiseReturnWorkflowService>.Instance);
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

    private void SetupPostedReceipts(int count)
    {
        _inventoryDocumentService.Setup(x => x.Search(It.Is<InventoryDocumentSearch>(s =>
                    s.MerchandiseReturnId == "r1" && s.Type == InventoryDocumentType.ReturnReceipt &&
                    s.Status == InventoryDocumentStatus.Posted), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<InventoryDocument>(
                Enumerable.Range(0, count).Select(_ => new InventoryDocument()), 0, 1));
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
    public async Task ChangeStatus_RejectWithPostedReceipt_ReturnsAlreadyReceived(MerchandiseReturnStatus status)
    {
        _stored = Return(MerchandiseReturnStatus.Accepted);
        SetupPostedReceipts(1);

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

        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturnNote>()),
            Times.Never);
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
    public async Task Delete_HoldsQuantityLock_AndDeletes()
    {
        var error = await _service.Delete("r1");

        Assert.IsNull(error);
        _resourceLock.Verify(x => x.Acquire(
            It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"), It.IsAny<CancellationToken>()),
            Times.Once);
        _merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(_stored), Times.Once);
    }
}
