using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
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
    IReturnShippingService returnShippingService,
    IMerchandiseReturnLabelService labelService,
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

        //the return method is checked before the lock: the provider may be asked about the form the customer filled
        //in, and no provider is called while the order's returns are held
        var shipping = await returnShippingService.Resolve(request, request.ReturnShipping);
        if (shipping.Errors.Count > 0)
        {
            foreach (var error in shipping.Errors)
                result.ShippingErrors.Add(error);
            return result;
        }

        var merchandiseReturns = new List<MerchandiseReturn>();
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
                foreach (var vendorItems in request.Order.OrderItems
                             .Where(orderItem => items.Any(x => x.OrderItemId == orderItem.Id))
                             .GroupBy(orderItem => orderItem.VendorId ?? ""))
                    merchandiseReturns.Add(await NewMerchandiseReturn(request, vendorItems.Key,
                        items.Where(x => vendorItems.Any(orderItem => orderItem.Id == x.OrderItemId)),
                        shipping.ByVendor.TryGetValue(vendorItems.Key, out var vendorShipping)
                            ? vendorShipping
                            : null));

                await InsertAllOrNone(merchandiseReturns);
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            logger.LogWarning(ex, "The returns of order {OrderId} stayed in use by another change", request.Order.Id);
            result.Error = MerchandiseReturnErrors.SubmitBusy;
            return result;
        }

        //the labels are created once the order's lock is let go: the label service takes it again to save, and the
        //lock is not reentrant
        foreach (var merchandiseReturn in merchandiseReturns)
            result.MerchandiseReturns.Add(await WithLabel(merchandiseReturn));

        return result;
    }

    /// <summary>
    ///     Creates the label of a saved return, or only its instructions; the return is saved whatever happens to its
    ///     label, which is created again later
    /// </summary>
    /// <returns>the return as saved with its label, for the messages the caller sends</returns>
    private async Task<MerchandiseReturn> WithLabel(MerchandiseReturn merchandiseReturn)
    {
        if (merchandiseReturn.ReturnShipping == null)
            return merchandiseReturn;

        try
        {
            var error = await labelService.CreateLabel(merchandiseReturn.Id);
            if (error != null)
                logger.LogWarning("The label of merchandise return {MerchandiseReturnId} was not created: {Error}",
                    merchandiseReturn.Id, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The label of merchandise return {MerchandiseReturnId} was not created",
                merchandiseReturn.Id);
        }

        return await ReadAgain(merchandiseReturn);
    }

    /// <summary>
    ///     Replaces the cancelled label of a return opened again: the old label, return code and tracking number
    ///     belong to a parcel that is never sent. The status change stands whatever happens to the new label
    /// </summary>
    /// <returns>the return as saved with its new label, for the messages the caller sends</returns>
    private async Task<MerchandiseReturn> WithNewLabel(MerchandiseReturn merchandiseReturn)
    {
        try
        {
            var error = await labelService.UpdateShipping(merchandiseReturn.Id, shipping =>
                {
                    shipping.LabelStatus = ReturnLabelStatus.Pending;
                    shipping.LabelError = null;
                    shipping.LabelDownloadId = null;
                    shipping.TrackingNumber = null;
                    shipping.ReturnCode = null;
                },
                //checked again on the return as stored: it may have been rejected or cancelled since
                stored => stored.MerchandiseReturnStatus == MerchandiseReturnStatus.Pending
                          && stored.ReturnShipping.LabelStatus == ReturnLabelStatus.Cancelled);
            if (error != null)
            {
                logger.LogWarning("The cancelled label of merchandise return {MerchandiseReturnId}, opened again, " +
                                  "was not replaced: {Error}", merchandiseReturn.Id, error);
                return await ReadAgain(merchandiseReturn);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The cancelled label of merchandise return {MerchandiseReturnId}, opened again, " +
                                "was not replaced", merchandiseReturn.Id);
            return await ReadAgain(merchandiseReturn);
        }

        return await WithLabel(merchandiseReturn);
    }

    /// <summary>
    ///     The return as the label service saved it, for the messages; the one in memory when it cannot be read
    /// </summary>
    private async Task<MerchandiseReturn> ReadAgain(MerchandiseReturn merchandiseReturn)
    {
        try
        {
            return await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturn.Id) ?? merchandiseReturn;
        }
        catch (Exception ex)
        {
            //the return is saved: its messages go out without the label's change rather than not at all
            logger.LogError(ex, "Merchandise return {MerchandiseReturnId} was not read again after its label",
                merchandiseReturn.Id);
            return merchandiseReturn;
        }
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
        string vendorId, IEnumerable<MerchandiseReturnSubmitItem> items, MerchandiseReturnShipping shipping)
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
            PickupAddress = request.PickupAddress,
            ReturnShipping = Copy(shipping)
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
                //the action is optional: none asked for leaves it empty
                RequestedAction = action != null ? action.GetTranslation(x => x.Name, request.LanguageId) : ""
            });
        }

        return merchandiseReturn;
    }

    /// <summary>
    ///     The return shipping of one return: the label and the provider's data of one vendor's parcel are never
    ///     those of another's. The addresses and the drop-off point are built for each vendor already
    /// </summary>
    private static MerchandiseReturnShipping Copy(MerchandiseReturnShipping shipping)
    {
        if (shipping == null)
            return null;

        return new MerchandiseReturnShipping {
            ProviderSystemName = shipping.ProviderSystemName,
            OptionCode = shipping.OptionCode,
            OptionName = shipping.OptionName,
            Cost = shipping.Cost,
            CurrencyRate = shipping.CurrencyRate,
            DropOffPoint = shipping.DropOffPoint,
            ReturnAddress = shipping.ReturnAddress,
            TrackingNumber = shipping.TrackingNumber,
            ReturnCode = shipping.ReturnCode,
            LabelDownloadId = shipping.LabelDownloadId,
            Instructions = shipping.Instructions,
            LabelStatusId = shipping.LabelStatusId,
            LabelError = shipping.LabelError,
            ProviderData = new Dictionary<string, string>(shipping.ProviderData ?? new Dictionary<string, string>())
        };
    }

    public Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status,
        string customerNote = null, string changedBy = null)
    {
        return ChangeStatus(merchandiseReturnId, status, customerNote, changedBy, null);
    }

    public async Task<string> ChangeStatus(string merchandiseReturnId, MerchandiseReturnStatus status,
        string customerNote, string changedBy, Func<MerchandiseReturn, bool> allowed)
    {
        MerchandiseReturn changed = null;
        var error = await UnderOrderLock(merchandiseReturnId, async stored =>
        {
            if (allowed != null && !allowed(stored))
                return MerchandiseReturnErrors.StatusChangeNotAllowed;

            var statusError = await StatusChangeError(stored, status);
            if (statusError != null)
                return statusError;

            stored.MerchandiseReturnStatus = status;
            //saved with the status, so the customer's timeline says when each one was reached
            stored.StatusHistory.Add(new MerchandiseReturnStatusChange { Status = status, CreatedOnUtc = DateTime.UtcNow });
            await merchandiseReturnService.UpdateMerchandiseReturn(stored);
            //for the staff: who changed the status; the note's date says when
            await merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
                MerchandiseReturnId = stored.Id,
                Note = $"Merchandise return status has been changed to {status} by " +
                       (string.IsNullOrWhiteSpace(changedBy) ? "system" : changedBy.Trim()),
                DisplayToCustomer = false
            });
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

        //the goods are not sent back: the label is cancelled with the carrier once the order's lock is let go, as
        //the label service takes it again to save; a label not cancelled leaves the status change as it is
        if (status is MerchandiseReturnStatus.Rejected or MerchandiseReturnStatus.Cancelled
            && changed.ReturnShipping?.LabelStatus == ReturnLabelStatus.Created)
        {
            try
            {
                await labelService.CancelLabel(changed.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "The label of merchandise return {MerchandiseReturnId} was not cancelled",
                    changed.Id);
            }

            //the message gives no link to a label that was cancelled
            changed = await ReadAgain(changed);
        }

        //opened again after its label was cancelled: the customer gets a new one to send the goods back with
        if (status == MerchandiseReturnStatus.Pending
            && changed.ReturnShipping?.LabelStatus == ReturnLabelStatus.Cancelled)
            changed = await WithNewLabel(changed);

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

    public async Task MarkReceived(string merchandiseReturnId, string changedBy = null)
    {
        //read without the lock, which ChangeStatus takes and which is not reentrant; a status changed in between is
        //checked again under it, and such a refusal leaves the return as it is
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn?.MerchandiseReturnStatus is not (MerchandiseReturnStatus.Pending
            or MerchandiseReturnStatus.Accepted))
            return;

        //the receipt is posted already, so a refusal is not the poster's error; the return, received by its stock,
        //can no longer be rejected, and the panel offers Received while a posted receipt of it exists
        var error = await ChangeStatus(merchandiseReturnId, MerchandiseReturnStatus.Received, changedBy: changedBy);
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

        //the refund completes the return once the money went back, or leaves it received when it did not
        if (stored.Refund is { RefundedOnUtc: null })
            return MerchandiseReturnErrors.RefundInProgress;

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
