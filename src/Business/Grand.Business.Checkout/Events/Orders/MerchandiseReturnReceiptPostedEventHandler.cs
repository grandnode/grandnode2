using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     A posted return receipt is the goods of a merchandise return back in stock: the return becomes received
/// </summary>
public class MerchandiseReturnReceiptPostedEventHandler : INotificationHandler<EntityUpdated<InventoryDocument>>
{
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;

    public MerchandiseReturnReceiptPostedEventHandler(
        IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
    }

    public async Task Handle(EntityUpdated<InventoryDocument> notification, CancellationToken cancellationToken)
    {
        var document = notification.Entity;
        if (document.DocumentTypeId != InventoryDocumentType.ReturnReceipt ||
            document.StatusId != InventoryDocumentStatus.Posted ||
            string.IsNullOrEmpty(document.MerchandiseReturnId))
            return;

        await _merchandiseReturnWorkflowService.MarkReceived(document.MerchandiseReturnId);
    }
}
