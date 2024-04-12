using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Discounts;

/// <summary>
///     Discount service
/// </summary>
public class DiscountService : IDiscountService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public DiscountService(ICacheBase cacheBase,
        IRepository<Discount> discountRepository,
        IRepository<DiscountCoupon> discountCouponRepository,
        IRepository<DiscountUsageHistory> discountUsageHistoryRepository,
        IMediator mediator, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _discountRepository = discountRepository;
        _discountCouponRepository = discountCouponRepository;
        _discountUsageHistoryRepository = discountUsageHistoryRepository;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<Discount> _discountRepository;
    private readonly IRepository<DiscountCoupon> _discountCouponRepository;
    private readonly IRepository<DiscountUsageHistory> _discountUsageHistoryRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a discount
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <returns>Discount</returns>
    public virtual Task<Discount> GetDiscountById(string discountId)
    {
        var key = string.Format(CacheKey.DISCOUNTS_BY_ID_KEY, discountId);
        return _cacheBase.GetAsync(key, () => _discountRepository.GetByIdAsync(discountId));
    }

    /// <summary>
    ///     Gets active discounts by context
    /// </summary>
    /// <returns>Discounts</returns>
    public virtual async Task<IList<Discount>> GetActiveDiscountsByContext(DiscountType discountType,
        string storeId = "", string currencyCode = "")
    {
        var key = string.Format(CacheKey.DISCOUNTS_CONTEXT_KEY, discountType, storeId, currencyCode);
        var allDiscounts = await _cacheBase.GetAsync(key, () => GetDiscountsQuery(discountType, storeId, currencyCode));

        var nowUtc = DateTime.UtcNow;
        var filteredDiscounts =
            allDiscounts.Where(d => d.IsEnabled &&
                                    (!d.StartDateUtc.HasValue || d.StartDateUtc <= nowUtc) &&
                                    (!d.EndDateUtc.HasValue || d.EndDateUtc >= nowUtc));

        return filteredDiscounts.ToList();
    }

    public virtual async Task<IList<Discount>> GetDiscountsQuery(DiscountType? discountType, string storeId = "",
        string currencyCode = "",
        string couponCode = "", string discountName = "")
    {
        var query = _discountRepository.Table.AsQueryable();

        if (discountType.HasValue) query = query.Where(d => d.DiscountTypeId == discountType);

        if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
            query = query.Where(d => !d.LimitedToStores || d.Stores.Contains(storeId));

        if (!string.IsNullOrEmpty(couponCode))
        {
            var couponDiscountId = _discountCouponRepository.Table
                .Where(x => x.CouponCode == couponCode)
                .Select(x => x.DiscountId)
                .FirstOrDefault();
            if (couponDiscountId != default)
                query = query.Where(d => d.Id == couponDiscountId);
            else
                query = query.Where(d => false);
        }

        if (!string.IsNullOrEmpty(discountName))
            query = query.Where(d =>
                d.Name != null && d.Name.Contains(discountName, StringComparison.CurrentCultureIgnoreCase));

        if (!string.IsNullOrEmpty(currencyCode)) query = query.Where(d => d.CurrencyCode == currencyCode);

        query = query.OrderBy(d => d.Name);

        return await Task.FromResult(query.ToList());
    }

    /// <summary>
    ///     Inserts a discount
    /// </summary>
    /// <param name="discount">Discount</param>
    public virtual async Task InsertDiscount(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        await _discountRepository.InsertAsync(discount);

        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(discount);
    }

    /// <summary>
    ///     Updates the discount
    /// </summary>
    /// <param name="discount">Discount</param>
    public virtual async Task UpdateDiscount(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        await _discountRepository.UpdateAsync(discount);

        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(discount);
    }

    /// <summary>
    ///     Delete discount
    /// </summary>
    /// <param name="discount">Discount</param>
    public virtual async Task DeleteDiscount(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        await _discountRepository.DeleteAsync(discount);

        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(discount);
    }


    /// <summary>
    ///     Get discount by coupon code
    /// </summary>
    /// <param name="couponCode">Coupon code</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Discount</returns>
    public virtual async Task<Discount> GetDiscountByCouponCode(string couponCode, bool showHidden = false)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return null;

        var coupon = await _discountCouponRepository.GetOneAsync(x => x.CouponCode == couponCode);
        if (coupon == null)
            return null;

        var discount = await GetDiscountById(coupon.DiscountId);
        return discount;
    }

    /// <summary>
    ///     Get all coupon codes for discount
    /// </summary>
    /// <param name="discountId"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public virtual async Task<IPagedList<DiscountCoupon>> GetAllCouponCodesByDiscountId(string discountId,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from d in _discountCouponRepository.Table
            select d;

        if (!string.IsNullOrEmpty(discountId))
            query = query.Where(duh => duh.DiscountId == discountId);
        query = query.OrderByDescending(c => c.CouponCode);

        return await PagedList<DiscountCoupon>.Create(query, pageIndex, pageSize);
    }


    /// <summary>
    ///     Gets a discount
    /// </summary>
    /// <returns>Discount</returns>
    public virtual Task<DiscountCoupon> GetDiscountCodeById(string id)
    {
        return _discountCouponRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Get discount code by discount code
    /// </summary>
    /// <param name="couponCode">Coupon code</param>
    /// <returns></returns>
    public virtual async Task<DiscountCoupon> GetDiscountCodeByCode(string couponCode)
    {
        return await _discountCouponRepository.GetOneAsync(x => x.CouponCode == couponCode);
    }


    /// <summary>
    ///     Delete discount code
    /// </summary>
    /// <param name="coupon"></param>
    public virtual async Task DeleteDiscountCoupon(DiscountCoupon coupon)
    {
        await _discountCouponRepository.DeleteAsync(coupon);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    /// <summary>
    ///     Insert discount code
    /// </summary>
    /// <param name="coupon"></param>
    public virtual async Task InsertDiscountCoupon(DiscountCoupon coupon)
    {
        await _discountCouponRepository.InsertAsync(coupon);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
    }

    /// <summary>
    ///     Update discount code - set as used or not
    /// </summary>
    /// <param name="couponCode"></param>
    /// <param name="used"></param>
    public virtual async Task DiscountCouponSetAsUsed(string couponCode, bool used)
    {
        if (string.IsNullOrEmpty(couponCode))
            return;

        var coupon = await GetDiscountCodeByCode(couponCode);
        if (coupon != null)
        {
            if (used)
            {
                coupon.Used = true;
                coupon.Qty++;
            }
            else
            {
                coupon.Qty -= 1;
                coupon.Used = coupon.Qty > 0;
            }

            await _discountCouponRepository.UpdateAsync(coupon);
        }
    }

    /// <summary>
    ///     Cancel discount if order was canceled or deleted
    /// </summary>
    /// <param name="orderId"></param>
    public virtual async Task CancelDiscount(string orderId)
    {
        var discountUsage = _discountUsageHistoryRepository.Table.Where(x => x.OrderId == orderId).ToList();
        foreach (var item in discountUsage)
        {
            await DiscountCouponSetAsUsed(item.CouponCode, false);
            item.Canceled = true;
            await UpdateDiscountUsageHistory(item);
        }
    }


    /// <summary>
    ///     Gets a discount usage history record
    /// </summary>
    /// <param name="discountUsageHistoryId">Discount usage history record identifier</param>
    /// <returns>Discount usage history</returns>
    public virtual Task<DiscountUsageHistory> GetDiscountUsageHistoryById(string discountUsageHistoryId)
    {
        return _discountUsageHistoryRepository.GetByIdAsync(discountUsageHistoryId);
    }

    /// <summary>
    ///     Insert discount usage history item
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history item</param>
    public virtual async Task InsertDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);

        await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

        //Support for coupon code
        await DiscountCouponSetAsUsed(discountUsageHistory.CouponCode, true);
        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);
        //event notification
        await _mediator.EntityInserted(discountUsageHistory);
    }


    /// <summary>
    ///     Update discount usage history item
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history item</param>
    public virtual async Task UpdateDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);

        await _discountUsageHistoryRepository.UpdateAsync(discountUsageHistory);

        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(discountUsageHistory);
    }

    /// <summary>
    ///     Delete discount usage history record
    /// </summary>
    /// <param name="discountUsageHistory">Discount usage history record</param>
    public virtual async Task DeleteDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);

        await _discountUsageHistoryRepository.DeleteAsync(discountUsageHistory);

        await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(discountUsageHistory);
    }

    /// <summary>
    ///     Get discount amount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="amount">Amount</param>
    /// <param name="currency">currency</param>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    public virtual async Task<double> GetDiscountAmount(Discount discount, Customer customer, Currency currency,
        Product product, double amount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        //calculate discount amount
        double result;
        if (!discount.CalculateByPlugin)
        {
            if (discount.UsePercentage)
                result = (float)amount * (float)discount.DiscountPercentage / 100f;
            else
                result = discount.DiscountAmount;
        }
        else
        {
            result = await _mediator.Send(new GetDiscountAmountProvider(discount, customer, product, currency, amount));
        }

        //validate maximum discount amount
        if (discount.UsePercentage &&
            discount.MaximumDiscountAmount.HasValue &&
            result > discount.MaximumDiscountAmount.Value)
            result = discount.MaximumDiscountAmount.Value;

        if (result < 0)
            result = 0;

        return result;
    }

    /// <summary>
    ///     Get preferred discount (with maximum discount value)
    /// </summary>
    /// <param name="discounts">A list of discounts to check</param>
    /// <param name="customer">customer</param>
    /// <param name="currency">currency</param>
    /// <param name="product"></param>
    /// <param name="amount">Amount</param>
    /// <returns>Preferred discount</returns>
    public virtual async Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
        IList<ApplyDiscount> discounts, Customer customer, Currency currency, Product product,
        double amount)
    {
        ArgumentNullException.ThrowIfNull(discounts);

        var appliedDiscount = new List<ApplyDiscount>();
        double discountAmount = 0;
        if (!discounts.Any())
            return (appliedDiscount, discountAmount);

        //check simple discounts
        foreach (var applieddiscount in discounts)
        {
            var discount = await GetDiscountById(applieddiscount.DiscountId);
            var currentDiscountValue = await GetDiscountAmount(discount, customer, currency, product, amount);
            if (!(currentDiscountValue > discountAmount)) continue;
            discountAmount = currentDiscountValue;
            appliedDiscount.Clear();
            appliedDiscount.Add(applieddiscount);
        }

        //cumulative discounts
        var cumulativeDiscounts = discounts.Where(x => x.IsCumulative).ToList();
        if (cumulativeDiscounts.Count <= 1) return (appliedDiscount, discountAmount);
        {
            double cumulativeDiscountAmount = 0;
            foreach (var item in cumulativeDiscounts)
            {
                var discount = await GetDiscountById(item.DiscountId);
                cumulativeDiscountAmount += await GetDiscountAmount(discount, customer, currency, product, amount);
            }

            if (!(cumulativeDiscountAmount > discountAmount)) return (appliedDiscount, discountAmount);
            discountAmount = cumulativeDiscountAmount;

            appliedDiscount.Clear();
            appliedDiscount.AddRange(cumulativeDiscounts);
        }

        return (appliedDiscount, discountAmount);
    }

    /// <summary>
    ///     Get preferred discount (with maximum discount value)
    /// </summary>
    /// <param name="discounts">A list of discounts to check</param>
    /// <param name="customer"></param>
    /// <param name="currency">currency</param>
    /// <param name="amount">Amount</param>
    /// <returns>Preferred discount</returns>
    public virtual async Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
        IList<ApplyDiscount> discounts,
        Customer customer,
        Currency currency,
        double amount)
    {
        return await GetPreferredDiscount(discounts, customer, currency, null, amount);
    }

    #endregion
}