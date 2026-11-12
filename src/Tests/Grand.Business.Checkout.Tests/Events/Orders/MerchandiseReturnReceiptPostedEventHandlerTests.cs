using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class MerchandiseReturnReceiptPostedEventHandlerTests
{
    private MerchandiseReturnReceiptPostedEventHandler _handler;
    private Mock<IMerchandiseReturnWorkflowService> _workflow;
    private Mock<ICustomerService> _customerService;

    [TestInitialize]
    public void Init()
    {
        _workflow = new Mock<IMerchandiseReturnWorkflowService>();
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(x => x.GetCustomerById("staff"))
            .ReturnsAsync(new Customer { Id = "staff", Email = "john@shop.com" });
        _handler = new MerchandiseReturnReceiptPostedEventHandler(_workflow.Object, _customerService.Object);
    }

    private static EntityUpdated<InventoryDocument> Updated(InventoryDocumentType type, InventoryDocumentStatus status,
        string merchandiseReturnId, string postedBy = "staff")
    {
        return new EntityUpdated<InventoryDocument>(new InventoryDocument {
            DocumentTypeId = type, StatusId = status, MerchandiseReturnId = merchandiseReturnId, PostedBy = postedBy
        });
    }

    [TestMethod]
    public async Task Handle_PostedReturnReceipt_MarksReceived()
    {
        await _handler.Handle(Updated(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "r1"),
            CancellationToken.None);

        //the staff note names who posted the receipt
        _workflow.Verify(x => x.MarkReceived("r1", "john@shop.com"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_PosterAccountGone_NamedByTheirId()
    {
        await _handler.Handle(Updated(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "r1", "gone"),
            CancellationToken.None);

        _workflow.Verify(x => x.MarkReceived("r1", "staff (gone)"), Times.Once);
    }

    [TestMethod]
    public async Task Handle_NoPoster_MarksReceivedBySystem()
    {
        await _handler.Handle(Updated(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "r1", null),
            CancellationToken.None);

        _workflow.Verify(x => x.MarkReceived("r1", null), Times.Once);
    }

    [TestMethod]
    [DataRow(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Draft, "r1")]
    [DataRow(InventoryDocumentType.Receipt, InventoryDocumentStatus.Posted, "r1")]
    [DataRow(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "")]
    public async Task Handle_DraftOtherTypeOrNoReturn_DoesNothing(InventoryDocumentType type,
        InventoryDocumentStatus status, string merchandiseReturnId)
    {
        await _handler.Handle(Updated(type, status, merchandiseReturnId), CancellationToken.None);

        _workflow.Verify(x => x.MarkReceived(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
