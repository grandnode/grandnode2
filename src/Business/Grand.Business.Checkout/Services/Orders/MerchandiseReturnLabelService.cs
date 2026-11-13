using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Services.Orders;

public class MerchandiseReturnLabelService(
    IMerchandiseReturnService merchandiseReturnService,
    IReturnShippingService returnShippingService,
    IDownloadService downloadService,
    IResourceLock resourceLock,
    ILogger<MerchandiseReturnLabelService> logger) : IMerchandiseReturnLabelService
{
    private const string ProviderNotInstalled = "provider not installed";
    private const string ProviderTimedOut = "timed out";

    /// <summary>
    ///     How long a provider call may run while the lock of the label is held, which ends with its lease
    /// </summary>
    protected virtual TimeSpan ProviderTimeout => TimeSpan.FromSeconds(IReturnShippingProvider.LabelCallTimeoutSeconds);

    public Task<string> CreateLabel(string merchandiseReturnId)
    {
        return UnderLabelLock(merchandiseReturnId, async stored =>
        {
            var shipping = stored.ReturnShipping;
            if (shipping == null)
                return MerchandiseReturnErrors.LabelNotAllowed;

            //a method without labels only gives the instructions, once
            if (shipping.LabelStatus == ReturnLabelStatus.NotRequired)
                return string.IsNullOrEmpty(shipping.Instructions) ? await CreateInstructions(stored) : null;

            //a click that waited for the lock finds the label of the click before it: no second label is bought
            if (shipping.LabelStatus == ReturnLabelStatus.Created)
                return null;

            if (shipping.LabelStatus is not (ReturnLabelStatus.Pending or ReturnLabelStatus.Failed)
                || stored.MerchandiseReturnStatus is not (MerchandiseReturnStatus.Pending
                    or MerchandiseReturnStatus.Accepted))
                return MerchandiseReturnErrors.LabelNotAllowed;

            return await CreateProviderLabel(stored);
        });
    }

    public async Task CancelLabel(string merchandiseReturnId)
    {
        var error = await UnderLabelLock(merchandiseReturnId, stored =>
            stored.ReturnShipping?.LabelStatus == ReturnLabelStatus.Created
                ? CancelProviderLabel(stored)
                : Task.FromResult<string>(null));

        //best effort: the status of the return is changed already, whatever happens to its label
        if (error != null)
            logger.LogWarning("The label of merchandise return {MerchandiseReturnId} was not cancelled: {Error}",
                merchandiseReturnId, error);
    }

    public Task<string> UpdateShipping(string merchandiseReturnId, Action<MerchandiseReturnShipping> apply,
        Func<MerchandiseReturn, bool> allowed = null)
    {
        ArgumentNullException.ThrowIfNull(apply);
        return UnderLabelLock(merchandiseReturnId, stored =>
            stored.ReturnShipping == null
                ? Task.FromResult(MerchandiseReturnErrors.NotFound)
                : SaveUnderOrderLock(stored, current =>
                {
                    //the stored return is written back unchanged when the change is no longer allowed
                    if (allowed == null || allowed(current))
                        apply(current.ReturnShipping);
                    return Task.CompletedTask;
                }));
    }

    private async Task<string> CreateInstructions(MerchandiseReturn stored)
    {
        var (result, error) = await RequestLabel(stored);
        if (error != null)
        {
            await InsertStaffNote(stored, $"The return instructions were not created: {error}");
            return null;
        }

        return await SaveUnderOrderLock(stored, current =>
        {
            current.ReturnShipping.Instructions = result.Instructions;
            current.ReturnShipping.ReturnCode = result.ReturnCode ?? current.ReturnShipping.ReturnCode;
            return Task.CompletedTask;
        });
    }

    private async Task<string> CreateProviderLabel(MerchandiseReturn stored)
    {
        var (result, error) = await RequestLabel(stored);

        MerchandiseReturn saved = null;
        var saveError = await SaveUnderOrderLock(stored, async current =>
        {
            var shipping = current.ReturnShipping;
            saved = current;
            if (error != null)
            {
                //the return stays as it is; the label is retried from the panel or by the customer
                shipping.LabelStatus = ReturnLabelStatus.Failed;
                shipping.LabelError = error;
                return;
            }

            if (result.LabelFile?.Content is { Length: > 0 })
                shipping.LabelDownloadId = await InsertLabelDownload(current, result.LabelFile);
            shipping.TrackingNumber = result.TrackingNumber ?? shipping.TrackingNumber;
            shipping.ReturnCode = result.ReturnCode ?? shipping.ReturnCode;
            shipping.Instructions = result.Instructions ?? shipping.Instructions;
            shipping.ProviderData ??= new Dictionary<string, string>();
            foreach (var (key, value) in result.ProviderData ?? [])
                shipping.ProviderData[key] = value;
            shipping.LabelStatus = ReturnLabelStatus.Created;
            shipping.LabelError = null;
        });
        if (saveError != null)
            return saveError;

        if (error != null)
        {
            await InsertStaffNote(saved, $"The return label was not created: {error}");
            return null;
        }

        //rejected or cancelled while the provider created the label: the label is not used, so it is cancelled
        //here, under the label lock still held (CancelLabel would wait for it)
        if (saved.MerchandiseReturnStatus is MerchandiseReturnStatus.Rejected or MerchandiseReturnStatus.Cancelled)
        {
            var cancelError = await CancelProviderLabel(saved);
            if (cancelError != null)
                logger.LogWarning("The label of merchandise return {MerchandiseReturnId}, rejected or cancelled " +
                                  "while it was created, was not cancelled: {Error}", saved.Id, cancelError);
        }

        return null;
    }

    private async Task<string> CancelProviderLabel(MerchandiseReturn stored)
    {
        var systemName = stored.ReturnShipping.ProviderSystemName;
        var provider = returnShippingService.LoadProviderBySystemName(systemName);
        string reason;
        if (provider == null)
        {
            logger.LogWarning("Return shipping provider {SystemName} of merchandise return {MerchandiseReturnId} " +
                              "is not installed to cancel its label", systemName, stored.Id);
            reason = ProviderNotInstalled;
        }
        else
        {
            Task<bool> call = null;
            try
            {
                call = provider.CancelLabel(stored);
                if (await call.WaitAsync(ProviderTimeout))
                    return await SaveUnderOrderLock(stored, current =>
                    {
                        current.ReturnShipping.LabelStatus = ReturnLabelStatus.Cancelled;
                        return Task.CompletedTask;
                    });

                logger.LogWarning("Return shipping provider {SystemName} did not cancel the label of merchandise " +
                                  "return {MerchandiseReturnId}", systemName, stored.Id);
                reason = "the provider did not cancel it";
            }
            catch (TimeoutException) when (call is { IsCompleted: false })
            {
                logger.LogWarning("Return shipping provider {SystemName} did not cancel the label of merchandise " +
                                  "return {MerchandiseReturnId} in time", systemName, stored.Id);
                reason = ProviderTimedOut;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Return shipping provider {SystemName} failed to cancel the label of merchandise " +
                                    "return {MerchandiseReturnId}", systemName, stored.Id);
                reason = ex.Message;
            }
        }

        await InsertStaffNote(stored, $"The return label was not cancelled: {reason}. Cancel it with the carrier.");
        return null;
    }

    /// <summary>
    ///     Asks the provider for the label; a provider that is missing, fails, throws or does not answer in time gives
    ///     the error instead
    /// </summary>
    private async Task<(ReturnLabelResult Result, string Error)> RequestLabel(MerchandiseReturn stored)
    {
        var systemName = stored.ReturnShipping.ProviderSystemName;
        var provider = returnShippingService.LoadProviderBySystemName(systemName);
        if (provider == null)
        {
            logger.LogWarning("Return shipping provider {SystemName} of merchandise return {MerchandiseReturnId} " +
                              "is not installed", systemName, stored.Id);
            return (null, ProviderNotInstalled);
        }

        Task<ReturnLabelResult> call = null;
        try
        {
            call = provider.CreateLabel(stored);
            var result = await call.WaitAsync(ProviderTimeout);
            if (result is { Success: true })
                return (result, null);

            var error = string.IsNullOrWhiteSpace(result?.Error) ? "the provider created no label" : result.Error;
            logger.LogWarning("Return shipping provider {SystemName} created no label for merchandise return " +
                              "{MerchandiseReturnId}: {Error}", systemName, stored.Id, error);
            return (null, error);
        }
        catch (TimeoutException) when (call is { IsCompleted: false })
        {
            logger.LogWarning("Return shipping provider {SystemName} did not create the label of merchandise " +
                              "return {MerchandiseReturnId} in time", systemName, stored.Id);
            return (null, ProviderTimedOut);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Return shipping provider {SystemName} failed to create the label of merchandise " +
                                "return {MerchandiseReturnId}", systemName, stored.Id);
            return (null, ex.Message);
        }
    }

    private async Task<string> InsertLabelDownload(MerchandiseReturn stored, ReturnLabelFile file)
    {
        var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "label" : file.FileName;
        var download = new Download {
            DownloadGuid = Guid.NewGuid(),
            UseDownloadUrl = false,
            DownloadBinary = file.Content,
            ContentType = file.ContentType,
            Filename = Path.GetFileNameWithoutExtension(fileName),
            Extension = Path.GetExtension(fileName),
            DownloadType = DownloadType.MerchandiseReturn,
            CustomerId = stored.CustomerId,
            ReferenceId = stored.Id
        };
        await downloadService.InsertDownload(download);
        return download.Id;
    }

    private Task InsertStaffNote(MerchandiseReturn stored, string note)
    {
        return merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
            MerchandiseReturnId = stored.Id,
            Note = note,
            DisplayToCustomer = false
        });
    }

    /// <summary>
    ///     Runs the change on the return as stored once the lock of its label is held: a label created by another call
    ///     that held the lock before is seen. The provider is called under this lock only, so the order's returns can
    ///     still be changed meanwhile
    /// </summary>
    private async Task<string> UnderLabelLock(string merchandiseReturnId, Func<MerchandiseReturn, Task<string>> change)
    {
        try
        {
            await using (await resourceLock.Acquire([MerchandiseReturnExtensions.LabelLockKey(merchandiseReturnId)]))
            {
                var stored = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
                if (stored == null)
                    return MerchandiseReturnErrors.NotFound;
                return await change(stored);
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            //another label call of the return held the lock for the whole wait: the user is asked to try again
            logger.LogWarning(ex, "The label of merchandise return {MerchandiseReturnId} stayed in use by another " +
                                  "change", merchandiseReturnId);
            return MerchandiseReturnErrors.Busy;
        }
    }

    /// <summary>
    ///     Saves the return shipping under the lock of the order as well, which every writer of the order's returns
    ///     holds, on the return as stored now: a status changed during the provider call is kept, and a full save of
    ///     the return never writes back the shipping it read before. Always taken inside the label lock, never the
    ///     other way round
    /// </summary>
    private async Task<string> SaveUnderOrderLock(MerchandiseReturn stored, Func<MerchandiseReturn, Task> apply)
    {
        try
        {
            await using (await resourceLock.Acquire([MerchandiseReturnExtensions.QuantityLockKey(stored.OrderId)]))
            {
                var current = await merchandiseReturnService.GetMerchandiseReturnById(stored.Id);
                if (current?.ReturnShipping == null)
                {
                    //deleted during the provider call: a label the provider created is left with the carrier
                    logger.LogWarning("Merchandise return {MerchandiseReturnId} was deleted before its return " +
                                      "shipping was saved", stored.Id);
                    return MerchandiseReturnErrors.NotFound;
                }

                await apply(current);
                await merchandiseReturnService.UpdateMerchandiseReturnShipping(current);
                return null;
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            logger.LogWarning(ex, "The returns of order {OrderId} stayed in use by another change; the return " +
                                  "shipping of merchandise return {MerchandiseReturnId} was not saved",
                stored.OrderId, stored.Id);
            return MerchandiseReturnErrors.Busy;
        }
    }
}
