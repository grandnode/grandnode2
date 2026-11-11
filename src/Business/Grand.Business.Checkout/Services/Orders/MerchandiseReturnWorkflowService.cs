using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Services.Orders;

public class MerchandiseReturnWorkflowService(
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IMediator mediator,
    IResourceLock resourceLock,
    IInventoryDocumentService inventoryDocumentService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
    ILogger<MerchandiseReturnWorkflowService> logger) : IMerchandiseReturnWorkflowService
{
    public async Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status,
        string customerNote = null)
    {
        MerchandiseReturn changed = null;
        var error = await UnderOrderLock(merchandiseReturnId, async stored =>
        {
            var statusError = await StatusChangeError(stored, status);
            if (statusError != null)
                return statusError;

            stored.MerchandiseReturnStatus = status;
            await merchandiseReturnService.UpdateMerchandiseReturn(stored);
            if (!string.IsNullOrWhiteSpace(customerNote))
                //the status message tells the customer about the change, so the note sends none of its own
                await merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
                    MerchandiseReturnId = stored.Id,
                    Note = customerNote.Trim(),
                    DisplayToCustomer = true
                });
            changed = stored;
            return null;
        });
        if (error != null)
            return error;

        var order = await orderService.GetOrderById(changed.OrderId);
        await merchandiseReturnMessageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(changed,
            order, order.CustomerLanguageId);
        return null;
    }

    public Task<string> Update(string merchandiseReturnId, Action<MerchandiseReturn> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        return UnderOrderLock(merchandiseReturnId, async stored =>
        {
            var status = stored.MerchandiseReturnStatusId;
            apply(stored);
            stored.MerchandiseReturnStatusId = status;
            await merchandiseReturnService.UpdateMerchandiseReturn(stored);
            return null;
        });
    }

    public Task<string> Delete(string merchandiseReturnId)
    {
        //the order's returned quantities are counted again under the lock its returns are saved with
        return UnderOrderLock(merchandiseReturnId, async stored =>
        {
            await merchandiseReturnService.DeleteMerchandiseReturn(stored);
            return null;
        });
    }

    public async Task MarkReceived(string merchandiseReturnId)
    {
        //read without the lock, which ChangeStatus takes and which is not reentrant; a status changed in between is
        //checked again under it, and such a refusal leaves the return as it is
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn?.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
            or MerchandiseReturnStatus.Accepted)
            await ChangeStatus(merchandiseReturnId, MerchandiseReturnStatus.Received);
    }

    /// <summary>
    ///     Runs the change on the return as stored once the lock of its order is held: another change that held the
    ///     lock before has been saved by then
    /// </summary>
    private async Task<string> UnderOrderLock(string merchandiseReturnId, Func<MerchandiseReturn, Task<string>> change)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return MerchandiseReturnErrors.NotFound;

        try
        {
            await using (await resourceLock.Acquire(
                             [MerchandiseReturnExtensions.QuantityLockKey(merchandiseReturn.OrderId)]))
            {
                var stored = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
                if (stored == null)
                    return MerchandiseReturnErrors.NotFound;
                return await change(stored);
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            //another change of the order's returns held the lock for the whole wait: the user is asked to try again
            logger.LogWarning(ex, "The returns of order {OrderId} stayed in use by another change",
                merchandiseReturn.OrderId);
            return MerchandiseReturnErrors.Busy;
        }
    }

    private async Task<string> StatusChangeError(MerchandiseReturn stored, MerchandiseReturnStatus status)
    {
        if (!stored.MerchandiseReturnStatus.CanChangeTo(status))
            return MerchandiseReturnErrors.StatusChangeNotAllowed;

        var takesQuantity = stored.MerchandiseReturnStatus.TakesQuantity();

        //a rejected or cancelled return opened again takes its quantity again, so that quantity must still be left
        if (!takesQuantity && status.TakesQuantity() && !await IsQuantityLeft(stored))
            return MerchandiseReturnErrors.QuantityExceeded;

        //rejecting or cancelling gives the quantity back to the customer, so none of it may be in stock already
        if (takesQuantity && !status.TakesQuantity() && await IsReceived(stored))
            return MerchandiseReturnErrors.AlreadyReceived;

        return null;
    }

    private async Task<bool> IsReceived(MerchandiseReturn merchandiseReturn)
    {
        var receipts = await inventoryDocumentService.Search(new InventoryDocumentSearch {
            Type = InventoryDocumentType.ReturnReceipt,
            Status = InventoryDocumentStatus.Posted,
            MerchandiseReturnId = merchandiseReturn.Id
        }, 0, 1);
        return receipts.Count > 0;
    }

    private async Task<bool> IsQuantityLeft(MerchandiseReturn merchandiseReturn)
    {
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        if (order == null)
            return false;

        var returnable = await mediator.Send(new GetReturnableQuantitiesQuery { Order = order, ForStaff = true });
        return merchandiseReturn.MerchandiseReturnItems.GroupBy(x => x.OrderItemId)
            .All(x => x.Sum(item => item.Quantity) <= returnable.GetValueOrDefault(x.Key));
    }
}
