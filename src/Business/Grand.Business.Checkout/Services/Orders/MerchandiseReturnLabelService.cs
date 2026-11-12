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

    public Task<string> CreateLabel(string merchandiseReturnId)
    {
        return UnderLabelLock(merchandiseReturnId, async stored =>
        {
            var shipping = stored.ReturnShipping;
            if (shipping == null)
                return MerchandiseReturnErrors.LabelNotAllowed;

            //a method without labels only gives the instructions, once
            if (shipping.LabelStatus == ReturnLabelStatus.NotRequired)
            {
                if (string.IsNullOrEmpty(shipping.Instructions))
                    await CreateInstructions(stored);
                return null;
            }

            //a click that waited for the lock finds the label of the click before it: no second label is bought
            if (shipping.LabelStatus == ReturnLabelStatus.Created)
                return null;

            if (shipping.LabelStatus is not (ReturnLabelStatus.Pending or ReturnLabelStatus.Failed)
                || stored.MerchandiseReturnStatus is MerchandiseReturnStatus.Rejected
                    or MerchandiseReturnStatus.Cancelled)
                return MerchandiseReturnErrors.LabelNotAllowed;

            await CreateProviderLabel(stored);
            return null;
        });
    }

    public async Task CancelLabel(string merchandiseReturnId)
    {
        var error = await UnderLabelLock(merchandiseReturnId, async stored =>
        {
            if (stored.ReturnShipping?.LabelStatus != ReturnLabelStatus.Created)
                return null;

            var provider = returnShippingService.LoadProviderBySystemName(stored.ReturnShipping.ProviderSystemName);
            bool cancelled;
            try
            {
                cancelled = provider != null && await provider.CancelLabel(stored);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Return shipping provider {SystemName} failed to cancel the label of merchandise " +
                                    "return {MerchandiseReturnId}", stored.ReturnShipping.ProviderSystemName,
                    stored.Id);
                cancelled = false;
            }

            if (!cancelled)
            {
                logger.LogWarning("The label of merchandise return {MerchandiseReturnId} was not cancelled",
                    stored.Id);
                await InsertStaffNote(stored, provider == null
                    ? $"The return label was not cancelled: {ProviderNotInstalled}."
                    : "The return label was not cancelled by the provider. Cancel it with the carrier.");
                return null;
            }

            stored.ReturnShipping.LabelStatus = ReturnLabelStatus.Cancelled;
            await merchandiseReturnService.UpdateMerchandiseReturnShipping(stored);
            return null;
        });

        //best effort: the status of the return is changed already, whatever happens to its label
        if (error != null)
            logger.LogWarning("The label of merchandise return {MerchandiseReturnId} was not cancelled: {Error}",
                merchandiseReturnId, error);
    }

    public Task<string> UpdateShipping(string merchandiseReturnId, Action<MerchandiseReturnShipping> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        return UnderLabelLock(merchandiseReturnId, async stored =>
        {
            if (stored.ReturnShipping == null)
                return MerchandiseReturnErrors.NotFound;

            apply(stored.ReturnShipping);
            await merchandiseReturnService.UpdateMerchandiseReturnShipping(stored);
            return null;
        });
    }

    private async Task CreateInstructions(MerchandiseReturn stored)
    {
        var (result, error) = await RequestLabel(stored);
        if (error != null)
        {
            await InsertStaffNote(stored, $"The return instructions were not created: {error}");
            return;
        }

        stored.ReturnShipping.Instructions = result.Instructions;
        stored.ReturnShipping.ReturnCode = result.ReturnCode ?? stored.ReturnShipping.ReturnCode;
        await merchandiseReturnService.UpdateMerchandiseReturnShipping(stored);
    }

    private async Task CreateProviderLabel(MerchandiseReturn stored)
    {
        var shipping = stored.ReturnShipping;
        var (result, error) = await RequestLabel(stored);
        if (error != null)
        {
            //the return stays as it is; the label is retried from the panel or by the customer
            shipping.LabelStatus = ReturnLabelStatus.Failed;
            shipping.LabelError = error;
            await merchandiseReturnService.UpdateMerchandiseReturnShipping(stored);
            await InsertStaffNote(stored, $"The return label was not created: {error}");
            return;
        }

        if (result.LabelFile?.Content is { Length: > 0 })
            shipping.LabelDownloadId = await InsertLabelDownload(stored, result.LabelFile);
        shipping.TrackingNumber = result.TrackingNumber ?? shipping.TrackingNumber;
        shipping.ReturnCode = result.ReturnCode ?? shipping.ReturnCode;
        shipping.Instructions = result.Instructions ?? shipping.Instructions;
        shipping.ProviderData ??= new Dictionary<string, string>();
        foreach (var (key, value) in result.ProviderData ?? [])
            shipping.ProviderData[key] = value;
        shipping.LabelStatus = ReturnLabelStatus.Created;
        shipping.LabelError = null;
        await merchandiseReturnService.UpdateMerchandiseReturnShipping(stored);
    }

    /// <summary>
    ///     Asks the provider for the label; a provider that is missing, fails or throws gives the error instead
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

        try
        {
            var result = await provider.CreateLabel(stored);
            if (result is { Success: true })
                return (result, null);

            var error = string.IsNullOrWhiteSpace(result?.Error) ? "the provider created no label" : result.Error;
            logger.LogWarning("Return shipping provider {SystemName} created no label for merchandise return " +
                              "{MerchandiseReturnId}: {Error}", systemName, stored.Id, error);
            return (null, error);
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
    ///     that held the lock before is seen. Never the lock of the order: it is not reentrant, and the provider is
    ///     called here while the order's returns can still be changed
    /// </summary>
    private async Task<string> UnderLabelLock(string merchandiseReturnId, Func<MerchandiseReturn, Task<string>> change)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return MerchandiseReturnErrors.NotFound;

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
}
