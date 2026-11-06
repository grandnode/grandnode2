using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.GiftVouchers;

/// <summary>
///     Gift voucher service
/// </summary>
public class GiftVoucherService : IGiftVoucherService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="giftVoucherRepository">Gift voucher context</param>
    /// <param name="mediator">Mediator</param>
    /// <param name="resourceLock">Lock</param>
    public GiftVoucherService(IRepository<GiftVoucher> giftVoucherRepository, IMediator mediator,
        IResourceLock resourceLock)
    {
        _giftVoucherRepository = giftVoucherRepository;
        _mediator = mediator;
        _resourceLock = resourceLock;
    }

    #endregion

    #region Fields

    private readonly IRepository<GiftVoucher> _giftVoucherRepository;
    private readonly IMediator _mediator;
    private readonly IResourceLock _resourceLock;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a gift voucher
    /// </summary>
    /// <param name="giftVoucherId">Gift voucher identifier</param>
    /// <returns>Gift voucher entry</returns>
    public virtual Task<GiftVoucher> GetGiftVoucherById(string giftVoucherId)
    {
        return _giftVoucherRepository.GetByIdAsync(giftVoucherId);
    }

    /// <summary>
    ///     Gets all gift vouchers
    /// </summary>
    /// <param name="purchasedWithOrderItemId">Associated order ID; null to load all records</param>
    /// <param name="storeId">Store identifier; null to load all records</param>
    /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
    /// <param name="isGiftVoucherActivated">Value indicating whether gift voucher is activated; null to load all records</param>
    /// <param name="giftVoucherCouponCode">Gift voucher coupon code; null to load all records</param>
    /// <param name="recipientName">Recipient name; null to load all records</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Gift vouchers</returns>
    public virtual async Task<IPagedList<GiftVoucher>> GetAllGiftVouchers(string purchasedWithOrderItemId = "",
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        bool? isGiftVoucherActivated = null, string giftVoucherCouponCode = null,
        string recipientName = null,
        int pageIndex = 0, int pageSize = int.MaxValue, string storeId = "")
    {
        var model = new GetGiftVoucherQuery {
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            Code = giftVoucherCouponCode,
            IsGiftVoucherActivated = isGiftVoucherActivated,
            PageIndex = pageIndex,
            PageSize = pageSize,
            PurchasedWithOrderItemId = purchasedWithOrderItemId,
            StoreId = storeId,
            RecipientName = recipientName
        };

        return await _mediator.Send(model);
    }

    public virtual async Task<IList<GiftVoucherUsageHistory>> GetAllGiftVoucherUsageHistory(string orderId = "")
    {
        var query = from g in _giftVoucherRepository.Table
            from h in g.GiftVoucherUsageHistory
            select h;

        query = query.Where(x => x.UsedWithOrderId == orderId);
        return await _giftVoucherRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Inserts a gift voucher
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    public virtual async Task InsertGiftVoucher(GiftVoucher giftVoucher)
    {
        ArgumentNullException.ThrowIfNull(giftVoucher);

        giftVoucher.Code = giftVoucher.Code.ToLowerInvariant();

        await _giftVoucherRepository.InsertAsync(giftVoucher);

        //event notification
        await _mediator.EntityInserted(giftVoucher);
    }

    /// <summary>
    ///     Updates the gift voucher
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    public virtual async Task UpdateGiftVoucher(GiftVoucher giftVoucher)
    {
        ArgumentNullException.ThrowIfNull(giftVoucher);

        giftVoucher.Code = giftVoucher.Code.ToLowerInvariant();

        await _giftVoucherRepository.UpdateAsync(giftVoucher);

        //event notification
        await _mediator.EntityUpdated(giftVoucher);
    }

    /// <summary>
    ///     Deletes a gift voucher
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    public virtual async Task DeleteGiftVoucher(GiftVoucher giftVoucher)
    {
        ArgumentNullException.ThrowIfNull(giftVoucher);

        await _giftVoucherRepository.DeleteAsync(giftVoucher);

        //event notification
        await _mediator.EntityDeleted(giftVoucher);
    }

    public virtual async Task<bool> TryRedeem(string giftVoucherId, double amount, string orderId)
    {
        await using var _ = await _resourceLock.Acquire([LockKey(giftVoucherId)]);

        var giftVoucher = await _giftVoucherRepository.GetByIdAsync(giftVoucherId);
        if (giftVoucher is not { IsGiftVoucherActivated: true })
            return false;

        //the balance is a sum of doubles: one redeemed down to zero may differ from the amount by noise alone
        if (Math.Round(giftVoucher.GetGiftVoucherRemainingAmount(), 6) < Math.Round(amount, 6))
            return false;

        var usage = new GiftVoucherUsageHistory {
            GiftVoucherId = giftVoucher.Id,
            UsedWithOrderId = orderId,
            UsedValue = amount,
            CreatedOnUtc = DateTime.UtcNow
        };
        //only the new entry is written, so a whole-document update made elsewhere cannot drop it
        await _giftVoucherRepository.AddToCollectionField(giftVoucher.Id, x => x.GiftVoucherUsageHistory, usage);
        giftVoucher.GiftVoucherUsageHistory.Add(usage);

        //event notification
        await _mediator.EntityUpdated(giftVoucher);

        return true;
    }

    public virtual async Task CancelRedemption(string giftVoucherId, string orderId)
    {
        await using var _ = await _resourceLock.Acquire([LockKey(giftVoucherId)]);

        await _giftVoucherRepository.RemoveCollectionFieldItem(giftVoucherId, x => x.GiftVoucherUsageHistory,
            x => x.UsedWithOrderId == orderId);

        //event notification
        var giftVoucher = await _giftVoucherRepository.GetByIdAsync(giftVoucherId);
        if (giftVoucher != null)
            await _mediator.EntityUpdated(giftVoucher);
    }

    /// <summary>
    ///     Gets gift vouchers by 'PurchasedWithOrderItemId'
    /// </summary>
    /// <param name="purchasedWithOrderItemId">Purchased with order item identifier</param>
    /// <returns>Gift voucher entries</returns>
    public virtual async Task<IList<GiftVoucher>> GetGiftVouchersByPurchasedWithOrderItemId(
        string purchasedWithOrderItemId)
    {
        if (string.IsNullOrEmpty(purchasedWithOrderItemId))
            return new List<GiftVoucher>();

        var query = from p in _giftVoucherRepository.Table
            select p;

        query = query.Where(gc =>
            gc.PurchasedWithOrderItem != null && gc.PurchasedWithOrderItem.Id == purchasedWithOrderItemId);
        query = query.OrderBy(gc => gc.Id);

        return await _giftVoucherRepository.ToListAsync(query);
    }

    /// <summary>
    ///     Generate new gift voucher code
    /// </summary>
    /// <returns>Result</returns>
    public virtual string GenerateGiftVoucherCode()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    private static string LockKey(string giftVoucherId)
    {
        return $"{nameof(GiftVoucher)}:{giftVoucherId}";
    }

    #endregion
}
