using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
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
    public async Task<MerchandiseReturnSubmitResult> Submit(MerchandiseReturnSubmitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new MerchandiseReturnSubmitResult();
        var items = request.Items.Where(x => x.Quantity > 0).ToList();
        if (items.Count == 0)
        {
            result.Error = MerchandiseReturnErrors.SubmitNothingToReturn;
            return result;
        }

        try
        {
            //two returns submitted at the same time for one order are checked and inserted one after the other
            await using (await resourceLock.Acquire([MerchandiseReturnExtensions.QuantityLockKey(request.Order.Id)]))
            {
                //never more than can still be returned, also when another return was submitted in the meantime: the
                //customer gets what they asked for or nothing, never a smaller return
                var returnable = await mediator.Send(new GetReturnableQuantitiesQuery { Order = request.Order });
                if (items.GroupBy(x => x.OrderItemId)
                    .Any(x => x.Sum(item => item.Quantity) > returnable.GetValueOrDefault(x.Key)))
                {
                    result.Error = MerchandiseReturnErrors.SubmitQuantityExceeded;
                    return result;
                }

                //each vendor handles the return of its own items
                var merchandiseReturns = new List<MerchandiseReturn>();
                foreach (var vendorItems in request.Order.OrderItems
                             .Where(orderItem => items.Any(x => x.OrderItemId == orderItem.Id))
                             .GroupBy(orderItem => orderItem.VendorId ?? ""))
                    merchandiseReturns.Add(await NewMerchandiseReturn(request, vendorItems.Key,
                        items.Where(x => vendorItems.Any(orderItem => orderItem.Id == x.OrderItemId))));

                await InsertAllOrNone(merchandiseReturns);
                foreach (var merchandiseReturn in merchandiseReturns)
                    result.MerchandiseReturns.Add(merchandiseReturn);
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            logger.LogWarning(ex, "The returns of order {OrderId} stayed in use by another change", request.Order.Id);
            result.Error = MerchandiseReturnErrors.SubmitBusy;
        }

        return result;
    }

    /// <summary>
    ///     Saves the returns of one submit, or none of them: the messages are sent once all are saved, so a return
    ///     left over from a submit that failed would take the customer's quantity with no one told about it
    /// </summary>
    private async Task InsertAllOrNone(IList<MerchandiseReturn> merchandiseReturns)
    {
        var inserted = new List<MerchandiseReturn>();
        try
        {
            foreach (var merchandiseReturn in merchandiseReturns)
            {
                //counted before the insert: one saved before its handlers failed is deleted as well
                inserted.Add(merchandiseReturn);
                await merchandiseReturnService.InsertMerchandiseReturn(merchandiseReturn);
            }
        }
        catch
        {
            foreach (var merchandiseReturn in inserted)
                try
                {
                    await merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Merchandise return {MerchandiseReturnId} of a submit that failed stayed saved",
                        merchandiseReturn.Id);
                }

            throw;
        }
    }

    private async Task<MerchandiseReturn> NewMerchandiseReturn(MerchandiseReturnSubmitRequest request,
        string vendorId, IEnumerable<MerchandiseReturnSubmitItem> items)
    {
        var merchandiseReturn = new MerchandiseReturn {
            StoreId = request.StoreId,
            OrderId = request.Order.Id,
            CustomerId = request.CustomerId,
            OwnerId = request.OwnerId,
            SeId = request.Order.SeId,
            VendorId = vendorId,
            CustomerComments = request.Comments,
            StaffNotes = string.Empty,
            MerchandiseReturnStatus = MerchandiseReturnStatus.Pending,
            PickupAddress = request.PickupAddress
        };
        if (request.PickupDate.HasValue)
            merchandiseReturn.PickupDate = request.PickupDate.Value;

        //only the reasons and actions the store offers: an id of another store's counts as none chosen
        var reasons = await merchandiseReturnService.GetAllMerchandiseReturnReasons(request.StoreId);
        var actions = await merchandiseReturnService.GetAllMerchandiseReturnActions(request.StoreId);
        foreach (var item in items)
        {
            var reason = reasons.FirstOrDefault(x => x.Id == item.ReasonId);
            var action = actions.FirstOrDefault(x => x.Id == item.ActionId);
            merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem {
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity,
                ReasonId = reason?.Id,
                ReasonForReturn = reason != null
                    ? reason.GetTranslation(x => x.Name, request.LanguageId)
                    : "not available",
                ActionId = action?.Id,
                RequestedAction = action != null
                    ? action.GetTranslation(x => x.Name, request.LanguageId)
                    : "not available"
            });
        }

        return merchandiseReturn;
    }

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

        //the status is saved: a message that could not be sent is logged, the change itself did not fail
        try
        {
            var order = await orderService.GetOrderById(changed.OrderId);
            if (order == null)
                logger.LogWarning("Merchandise return {MerchandiseReturnId} changed status, but its order {OrderId} " +
                                  "was not found to tell the customer", changed.Id, changed.OrderId);
            else
                await merchandiseReturnMessageProviderService.SendMerchandiseReturnStatusChangedCustomerMessage(
                    changed, order, order.CustomerLanguageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The status changed message of merchandise return {MerchandiseReturnId} was not sent",
                changed.Id);
        }

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
            //deleting gives the quantity back to the customer, like rejecting does: not once the goods are back
            if (stored.MerchandiseReturnStatus is MerchandiseReturnStatus.Received or MerchandiseReturnStatus.Completed
                || await IsReceived(stored))
                return MerchandiseReturnErrors.AlreadyReceived;

            await merchandiseReturnService.DeleteMerchandiseReturn(stored);
            return null;
        });
    }

    public async Task MarkReceived(string merchandiseReturnId)
    {
        //read without the lock, which ChangeStatus takes and which is not reentrant; a status changed in between is
        //checked again under it, and such a refusal leaves the return as it is
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn?.MerchandiseReturnStatus is not (MerchandiseReturnStatus.Pending
            or MerchandiseReturnStatus.Accepted))
            return;

        //the receipt is posted already, so a refusal is not the poster's error; the return, received by its stock,
        //can no longer be rejected and is marked received by hand
        var error = await ChangeStatus(merchandiseReturnId, MerchandiseReturnStatus.Received);
        if (error != null)
            logger.LogWarning("A return receipt of merchandise return {MerchandiseReturnId} was posted, but the " +
                              "return was not marked received: {Error}", merchandiseReturnId, error);
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

    /// <summary>
    ///     Whether goods of the return are in stock: a receipt posted, or one whose posting booked some lines. A
    ///     receipt books under the lock held here, so none starts booking while the status is changed
    /// </summary>
    private Task<bool> IsReceived(MerchandiseReturn merchandiseReturn)
    {
        return inventoryDocumentService.IsReturnReceived(merchandiseReturn.Id);
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
