using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Discounts
{
    /// <summary>
    /// Discount service
    /// </summary>
    public partial class DiscountService : IDiscountService
    {

        #region Fields

        private readonly IRepository<Discount> _discountRepository;
        private readonly IRepository<DiscountCoupon> _discountCouponRepository;
        private readonly IRepository<DiscountUsageHistory> _discountUsageHistoryRepository;
        private readonly ITranslationService _translationService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly IEnumerable<IDiscountProvider> _discountProviders;
        private readonly IEnumerable<IDiscountAmountProvider> _discountAmountProviders;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public DiscountService(ICacheBase cacheBase,
            IRepository<Discount> discountRepository,
            IRepository<DiscountCoupon> discountCouponRepository,
            IRepository<DiscountUsageHistory> discountUsageHistoryRepository,
            ITranslationService translationService,
            IWorkContext workContext,
            IEnumerable<IDiscountProvider> discountProviders,
            IEnumerable<IDiscountAmountProvider> discountAmountProviders,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _discountRepository = discountRepository;
            _discountCouponRepository = discountCouponRepository;
            _discountUsageHistoryRepository = discountUsageHistoryRepository;
            _translationService = translationService;
            _workContext = workContext;
            _discountProviders = discountProviders;
            _discountAmountProviders = discountAmountProviders;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a discount
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <returns>Discount</returns>
        public virtual Task<Discount> GetDiscountById(string discountId)
        {
            string key = string.Format(CacheKey.DISCOUNTS_BY_ID_KEY, discountId);
            return _cacheBase.GetAsync(key, () => _discountRepository.GetByIdAsync(discountId));
        }

        /// <summary>
        /// Gets all discounts
        /// </summary>       
        /// <returns>Discounts</returns>
        public virtual async Task<IList<Discount>> GetAllDiscounts(DiscountType? discountType,
            string storeId = "", string currencyCode = "", string couponCode = "", string discountName = "", bool showHidden = false)
        {
            string key = string.Format(CacheKey.DISCOUNTS_ALL_KEY, showHidden, storeId, currencyCode, couponCode, discountName);
            var result = await _cacheBase.GetAsync(key, async () =>
            {
                var query = from m in _discountRepository.Table
                            select m;
                if (!showHidden)
                {
                    var nowUtc = DateTime.UtcNow;
                    query = query.Where(d =>
                        (!d.StartDateUtc.HasValue || d.StartDateUtc <= nowUtc)
                        && (!d.EndDateUtc.HasValue || d.EndDateUtc >= nowUtc)
                        && d.IsEnabled);
                }
                if (!string.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                {
                    //Store acl
                    query = from p in query
                            where !p.LimitedToStores || p.Stores.Contains(storeId)
                            select p;
                }
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var _coupon = _discountCouponRepository.Table.FirstOrDefault(x => x.CouponCode == couponCode);
                    if (_coupon != null)
                        query = query.Where(d => d.Id == _coupon.DiscountId);
                }
                if (!string.IsNullOrEmpty(discountName))
                {
                    query = query.Where(d => d.Name != null && d.Name.ToLower().Contains(discountName.ToLower()));
                }
                if (!string.IsNullOrEmpty(currencyCode))
                {
                    query = query.Where(d => d.CurrencyCode == currencyCode);
                }
                query = query.OrderBy(d => d.Name);

                var discounts = await Task.FromResult(query.ToList());
                return discounts;
            });
            if (discountType.HasValue)
            {
                result = result.Where(d => d.DiscountTypeId == discountType.Value).ToList();
            }
            return result;
        }

        /// <summary>
        /// Inserts a discount
        /// </summary>
        /// <param name="discount">Discount</param>
        public virtual async Task InsertDiscount(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            await _discountRepository.InsertAsync(discount);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(discount);
        }

        /// <summary>
        /// Updates the discount
        /// </summary>
        /// <param name="discount">Discount</param>
        public virtual async Task UpdateDiscount(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            foreach (var req in discount.DiscountRules)
            {
                req.DiscountId = discount.Id;
            }

            await _discountRepository.UpdateAsync(discount);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(discount);
        }

        /// <summary>
        /// Delete discount
        /// </summary>
        /// <param name="discount">Discount</param>
        public virtual async Task DeleteDiscount(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            var usagehistory = await GetAllDiscountUsageHistory(discount.Id);
            if (usagehistory.Count > 0)
                throw new ArgumentNullException("discount was used and have a history");

            await _discountRepository.DeleteAsync(discount);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(discount);
        }

        /// <summary>
        /// Delete discount requirement
        /// </summary>
        /// <param name="discountRequirement">Discount requirement</param>
        public virtual async Task DeleteDiscountRequirement(DiscountRule discountRequirement)
        {
            if (discountRequirement == null)
                throw new ArgumentNullException(nameof(discountRequirement));

            var discount = await _discountRepository.GetByIdAsync(discountRequirement.DiscountId);
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));
            var req = discount.DiscountRules.FirstOrDefault(x => x.Id == discountRequirement.Id);
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            discount.DiscountRules.Remove(req);
            await UpdateDiscount(discount);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(discountRequirement);
        }

        /// <summary>
        /// Load discount by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found discount</returns>
        public virtual IDiscountProvider LoadDiscountProviderBySystemName(string systemName)
        {
            var discountPlugins = LoadAllDiscountProviders();
            foreach (var discountPlugin in discountPlugins)
            {
                var rules = discountPlugin.GetRequirementRules();

                if (!rules.Any(x => x.SystemName == systemName))
                    continue;
                return discountPlugin;
            }
            return null;
        }

        /// <summary>
        /// Load all discount providers
        /// </summary>
        /// <returns>Discount providers</returns>
        public virtual IList<IDiscountProvider> LoadAllDiscountProviders()
        {
            return _discountProviders.ToList();
        }


        /// <summary>
        /// Get discount by coupon code
        /// </summary>
        /// <param name="couponCode">Coupon code</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Discount</returns>
        public virtual async Task<Discount> GetDiscountByCouponCode(string couponCode, bool showHidden = false)
        {
            if (String.IsNullOrWhiteSpace(couponCode))
                return null;

            var query = _discountCouponRepository.Table.Where(x => x.CouponCode == couponCode).ToList();

            var coupon = query.FirstOrDefault();
            if (coupon == null)
                return null;

            var discount = await GetDiscountById(coupon.DiscountId);
            return discount;
        }

        /// <summary>
        /// Exist coupon code in discount
        /// </summary>
        /// <param name="couponCode"></param>
        /// <param name="discountId"></param>
        /// <returns></returns>
        public virtual async Task<bool> ExistsCodeInDiscount(string couponCode, string discountId, bool? used)
        {
            if (String.IsNullOrWhiteSpace(couponCode))
                return false;

            var query = _discountCouponRepository.Table.Where(x => x.CouponCode == couponCode
                            && x.DiscountId == discountId);

            if (used.HasValue)
                query = query.Where(x => x.Used == used.Value);

            var result = await Task.FromResult(query.ToList());

            if (result.Any())
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get all coupon codes for discount
        /// </summary>
        /// <param name="discountId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public virtual async Task<IPagedList<DiscountCoupon>> GetAllCouponCodesByDiscountId(string discountId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from d in _discountCouponRepository.Table
                        select d;

            if (!string.IsNullOrEmpty(discountId))
                query = query.Where(duh => duh.DiscountId == discountId);
            query = query.OrderByDescending(c => c.CouponCode);

            return await PagedList<DiscountCoupon>.Create(query, pageIndex, pageSize);
        }


        /// <summary>
        /// Gets a discount
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <returns>Discount</returns>
        public virtual Task<DiscountCoupon> GetDiscountCodeById(string id)
        {
            return _discountCouponRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Get discount code by discount code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<DiscountCoupon> GetDiscountCodeByCode(string couponCode)
        {
            var query = await Task.FromResult(_discountCouponRepository.Table.Where(x => x.CouponCode == couponCode).ToList());
            return query.FirstOrDefault();
        }


        /// <summary>
        /// Delete discount code
        /// </summary>
        /// <param name="coupon"></param>
        public virtual async Task DeleteDiscountCoupon(DiscountCoupon coupon)
        {
            await _discountCouponRepository.DeleteAsync(coupon);
        }

        /// <summary>
        /// Insert discount code
        /// </summary>
        /// <param name="coupon"></param>
        public virtual async Task InsertDiscountCoupon(DiscountCoupon coupon)
        {
            await _discountCouponRepository.InsertAsync(coupon);
        }

        /// <summary>
        /// Update discount code - set as used or not
        /// </summary>
        /// <param name="coupon"></param>
        public virtual async Task DiscountCouponSetAsUsed(string couponCode, bool used)
        {
            if (string.IsNullOrEmpty(couponCode))
                return;

            var coupon = await GetDiscountCodeByCode(couponCode);
            if (coupon != null)
            {
                if (used)
                {
                    coupon.Used = used;
                    coupon.Qty++;
                }
                else
                {
                    coupon.Qty = coupon.Qty - 1;
                    coupon.Used = coupon.Qty > 0;
                }
                await _discountCouponRepository.UpdateAsync(coupon);
            }
        }

        /// <summary>
        /// Cancel discount if order was canceled or deleted
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
        /// Validate discount
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <returns>Discount validation result</returns>
        public virtual async Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            string[] couponCodesToValidate = null;
            if (customer != null)
                couponCodesToValidate = customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);

            return await ValidateDiscount(discount, customer, currency, couponCodesToValidate);
        }

        /// <summary>
        /// Validate discount
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="couponCodeToValidate">Coupon code</param>
        /// <returns>Discount validation result</returns>
        public virtual async Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency, string couponCodeToValidate)
        {
            if (!String.IsNullOrEmpty(couponCodeToValidate))
            {
                return await ValidateDiscount(discount, customer, currency, new string[] { couponCodeToValidate });
            }
            else
                return await ValidateDiscount(discount, customer, currency, new string[0]);

        }

        /// <summary>
        /// Validate discount
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="customer">Customer</param>
        /// <param name="currency">Currency</param>
        /// <param name="couponCodesToValidate">Coupon codes</param>
        /// <returns>Discount validation result</returns>
        public virtual async Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Currency currency, string[] couponCodesToValidate)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var result = new DiscountValidationResult();

            //is enabled and use the same currency
            if (!discount.IsEnabled || discount.CurrencyCode != currency.CurrencyCode)
                return result;

            //do not allow use discount in the current store
            if (discount.LimitedToStores && !discount.Stores.Any(x => _workContext.CurrentStore.Id == x))
            {
                result.UserError = _translationService.GetResource("ShoppingCart.Discount.CannotBeUsedInStore");
                return result;
            }

            //check coupon code
            if (discount.RequiresCouponCode)
            {
                if (couponCodesToValidate == null || !couponCodesToValidate.Any())
                    return result;
                var exists = false;
                foreach (var item in couponCodesToValidate)
                {
                    if (discount.Reused)
                    {
                        if (await ExistsCodeInDiscount(item, discount.Id, null))
                        {
                            result.CouponCode = item;
                            exists = true;
                        }
                    }
                    else
                    {
                        if (await ExistsCodeInDiscount(item, discount.Id, false))
                        {
                            result.CouponCode = item;
                            exists = true;
                        }
                    }
                }
                if (!exists)
                    return result;
            }

            if (discount.DiscountTypeId == DiscountType.AssignedToOrderSubTotal ||
                discount.DiscountTypeId == DiscountType.AssignedToOrderTotal)
            {
                var cart = customer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                    .ToList();

                var hasGiftVouchers = cart.Any(x => x.IsGiftVoucher);
                if (hasGiftVouchers)
                {
                    result.UserError = _translationService.GetResource("ShoppingCart.Discount.CannotBeUsedWithGiftVouchers");
                    return result;
                }
            }
            //time range check
            DateTime now = DateTime.UtcNow;
            if (discount.StartDateUtc.HasValue)
            {
                DateTime startDate = DateTime.SpecifyKind(discount.StartDateUtc.Value, DateTimeKind.Utc);
                if (startDate.CompareTo(now) > 0)
                {
                    result.UserError = _translationService.GetResource("ShoppingCart.Discount.NotStartedYet");
                    return result;
                }
            }
            if (discount.EndDateUtc.HasValue)
            {
                DateTime endDate = DateTime.SpecifyKind(discount.EndDateUtc.Value, DateTimeKind.Utc);
                if (endDate.CompareTo(now) < 0)
                {
                    result.UserError = _translationService.GetResource("ShoppingCart.Discount.Expired");
                    return result;
                }
            }

            //discount limitation - n times and n times per user
            switch (discount.DiscountLimitationId)
            {
                case DiscountLimitationType.NTimes:
                    {
                        var usedTimes = await GetAllDiscountUsageHistory(discount.Id, null, null, false, 0, 1);
                        if (usedTimes.TotalCount >= discount.LimitationTimes)
                            return result;
                    }
                    break;
                case DiscountLimitationType.NTimesPerUser:
                    {
                        var usedTimes = await GetAllDiscountUsageHistory(discount.Id, customer.Id, null, false, 0, 1);
                        if (usedTimes.TotalCount >= discount.LimitationTimes)
                        {
                            result.UserError = _translationService.GetResource("ShoppingCart.Discount.CannotBeUsedAnymore");
                            return result;
                        }
                    }
                    break;
                case DiscountLimitationType.Nolimits:
                default:
                    break;
            }

            //discount requirements
            var requirements = discount.DiscountRules.ToList();
            foreach (var req in requirements)
            {
                //load a plugin
                var discountRequirementPlugin = LoadDiscountProviderBySystemName(req.DiscountRequirementRuleSystemName);

                if (discountRequirementPlugin == null)
                    continue;

                if (!discountRequirementPlugin.IsAuthenticateStore(_workContext.CurrentStore))
                    continue;

                var ruleRequest = new DiscountRuleValidationRequest
                {
                    DiscountRequirementId = req.Id,
                    DiscountId = req.DiscountId,
                    Customer = customer,
                    Store = _workContext.CurrentStore
                };

                var singleRequirementRule = discountRequirementPlugin.GetRequirementRules().Single(x => x.SystemName == req.DiscountRequirementRuleSystemName);
                var ruleResult = await singleRequirementRule.CheckRequirement(ruleRequest);
                if (!ruleResult.IsValid)
                {
                    result.UserError = ruleResult.UserError;
                    return result;
                }
            }

            result.IsValid = true;
            return result;

        }
        /// <summary>
        /// Gets a discount usage history record
        /// </summary>
        /// <param name="discountUsageHistoryId">Discount usage history record identifier</param>
        /// <returns>Discount usage history</returns>
        public virtual Task<DiscountUsageHistory> GetDiscountUsageHistoryById(string discountUsageHistoryId)
        {
            return _discountUsageHistoryRepository.GetByIdAsync(discountUsageHistoryId);
        }

        /// <summary>
        /// Gets all discount usage history records
        /// </summary>
        /// <param name="discountId">Discount identifier; use null to load all records</param>
        /// <param name="customerId">Customer identifier; use null to load all records</param>
        /// <param name="orderId">Order identifier; null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Discount usage history records</returns>
        public virtual async Task<IPagedList<DiscountUsageHistory>> GetAllDiscountUsageHistory(string discountId = "",
            string customerId = "", string orderId = "", bool? canceled = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from d in _discountUsageHistoryRepository.Table
                        select d;

            if (!string.IsNullOrEmpty(discountId))
                query = query.Where(duh => duh.DiscountId == discountId);
            if (!string.IsNullOrEmpty(customerId))
                query = query.Where(duh => duh.CustomerId == customerId);
            if (!string.IsNullOrEmpty(orderId))
                query = query.Where(duh => duh.OrderId == orderId);
            if (canceled.HasValue)
                query = query.Where(duh => duh.Canceled == canceled.Value);
            query = query.OrderByDescending(c => c.CreatedOnUtc);

            return await PagedList<DiscountUsageHistory>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Insert discount usage history item
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history item</param>
        public virtual async Task InsertDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
        {
            if (discountUsageHistory == null)
                throw new ArgumentNullException(nameof(discountUsageHistory));

            await _discountUsageHistoryRepository.InsertAsync(discountUsageHistory);

            //Support for couponcode
            await DiscountCouponSetAsUsed(discountUsageHistory.CouponCode, true);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(discountUsageHistory);
        }


        /// <summary>
        /// Update discount usage history item
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history item</param>
        public virtual async Task UpdateDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
        {
            if (discountUsageHistory == null)
                throw new ArgumentNullException(nameof(discountUsageHistory));

            await _discountUsageHistoryRepository.UpdateAsync(discountUsageHistory);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(discountUsageHistory);
        }

        /// <summary>
        /// Delete discount usage history record
        /// </summary>
        /// <param name="discountUsageHistory">Discount usage history record</param>
        public virtual async Task DeleteDiscountUsageHistory(DiscountUsageHistory discountUsageHistory)
        {
            if (discountUsageHistory == null)
                throw new ArgumentNullException(nameof(discountUsageHistory));

            await _discountUsageHistoryRepository.DeleteAsync(discountUsageHistory);

            await _cacheBase.RemoveByPrefix(CacheKey.DISCOUNTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(discountUsageHistory);
        }

        /// <summary>
        /// Get discount amount from plugin
        /// </summary>
        /// <param name="discount">Discount</param>
        /// <param name="amount">Amount</param>
        /// <param name="currency">currency</param>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        public async Task<double> GetDiscountAmount(Discount discount, Customer customer, Currency currency, Product product, double amount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            //calculate discount amount
            double result = 0;
            if (!discount.CalculateByPlugin)
            {
                if (discount.UsePercentage)
                    result = (double)((((float)amount) * ((float)discount.DiscountPercentage)) / 100f);
                else
                {
                    result = discount.DiscountAmount;
                }
            }
            else
            {
                result = await GetDiscountAmountProvider(discount, customer, product, amount);
            }

            //validate maximum disocunt amount
            if (discount.UsePercentage &&
                discount.MaximumDiscountAmount.HasValue &&
                result > discount.MaximumDiscountAmount.Value)
                result = discount.MaximumDiscountAmount.Value;

            if (result < 0)
                result = 0;

            return result;
        }

        /// <summary>
        /// Get preferred discount (with maximum discount value)
        /// </summary>
        /// <param name="discounts">A list of discounts to check</param>
        /// <param name="customer">customer</param>
        /// <param name="currency">currency</param>
        /// <param name="product"></param>
        /// <param name="amount">Amount</param>
        /// <param name="discountAmount"></param>
        /// <returns>Preferred discount</returns>
        public virtual async Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
            IList<ApplyDiscount> discounts, Customer customer, Currency currency, Product product,
            double amount)
        {
            if (discounts == null)
                throw new ArgumentNullException(nameof(discounts));

            var appliedDiscount = new List<ApplyDiscount>();
            double discountAmount = 0;
            if (!discounts.Any())
                return (appliedDiscount, discountAmount);

            //check simple discounts
            foreach (var applieddiscount in discounts)
            {
                var discount = await GetDiscountById(applieddiscount.DiscountId);
                double currentDiscountValue = await GetDiscountAmount(discount, customer, currency, product, amount);
                if (currentDiscountValue > discountAmount)
                {
                    discountAmount = currentDiscountValue;
                    appliedDiscount.Clear();
                    appliedDiscount.Add(applieddiscount);
                }
            }
            //cumulative discounts
            var cumulativeDiscounts = discounts.Where(x => x.IsCumulative).ToList();
            if (cumulativeDiscounts.Count > 1)
            {
                double cumulativeDiscountAmount = 0;
                foreach (var item in cumulativeDiscounts)
                {
                    var discount = await GetDiscountById(item.DiscountId);
                    cumulativeDiscountAmount += await GetDiscountAmount(discount, customer, currency, product, amount);
                }
                if (cumulativeDiscountAmount > discountAmount)
                {
                    discountAmount = cumulativeDiscountAmount;

                    appliedDiscount.Clear();
                    appliedDiscount.AddRange(cumulativeDiscounts);
                }
            }

            return (appliedDiscount, discountAmount);
        }
        /// <summary>
        /// Get preferred discount (with maximum discount value)
        /// </summary>
        /// <param name="discounts">A list of discounts to check</param>
        /// <param name="customer"></param>
        /// <param name="currency">currency</param>
        /// <param name="amount">Amount</param>
        /// <param name="discountAmount"></param>
        /// <returns>Preferred discount</returns>
        public virtual async Task<(List<ApplyDiscount> appliedDiscount, double discountAmount)> GetPreferredDiscount(
            IList<ApplyDiscount> discounts,
            Customer customer,
            Currency currency,
            double amount)
        {
            return await GetPreferredDiscount(discounts, customer, currency, null, amount);
        }

        /// <summary>
        /// Get amount from discount amount provider 
        /// </summary>
        /// <param name="discount"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual async Task<double> GetDiscountAmountProvider(Discount discount, Customer customer, Product product, double amount)
        {
            var discountAmountProvider = LoadDiscountAmountProviderBySystemName(discount.DiscountPluginName);
            if (discountAmountProvider == null)
                return 0;
            return await discountAmountProvider.DiscountAmount(discount, customer, product, amount);
        }


        public virtual IDiscountAmountProvider LoadDiscountAmountProviderBySystemName(string systemName)
        {
            return _discountAmountProviders.FirstOrDefault(x => x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all discount amount providers
        /// </summary>
        /// <returns></returns>
        public IList<IDiscountAmountProvider> LoadDiscountAmountProviders()
        {
            return _discountAmountProviders.ToList();
        }

        #endregion
    }
}
