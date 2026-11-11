using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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

    [TestInitialize]
    public void Init()
    {
        _workflow = new Mock<IMerchandiseReturnWorkflowService>();
        _handler = new MerchandiseReturnReceiptPostedEventHandler(_workflow.Object);
    }

    private static EntityUpdated<InventoryDocument> Updated(InventoryDocumentType type, InventoryDocumentStatus status,
        string merchandiseReturnId)
    {
        return new EntityUpdated<InventoryDocument>(new InventoryDocument {
            DocumentTypeId = type, StatusId = status, MerchandiseReturnId = merchandiseReturnId
        });
    }

    [TestMethod]
    public async Task Handle_PostedReturnReceipt_MarksReceived()
    {
        await _handler.Handle(Updated(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "r1"),
            CancellationToken.None);

        _workflow.Verify(x => x.MarkReceived("r1"), Times.Once);
    }

    [TestMethod]
    [DataRow(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Draft, "r1")]
    [DataRow(InventoryDocumentType.Receipt, InventoryDocumentStatus.Posted, "r1")]
    [DataRow(InventoryDocumentType.ReturnReceipt, InventoryDocumentStatus.Posted, "")]
    public async Task Handle_DraftOtherTypeOrNoReturn_DoesNothing(InventoryDocumentType type,
        InventoryDocumentStatus status, string merchandiseReturnId)
    {
        await _handler.Handle(Updated(type, status, merchandiseReturnId), CancellationToken.None);

        _workflow.Verify(x => x.MarkReceived(It.IsAny<string>()), Times.Never);
    }
}
