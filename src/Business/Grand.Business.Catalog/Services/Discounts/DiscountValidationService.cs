using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Discounts;

public class DiscountValidationService : IDiscountValidationService
{
    private readonly IRepository<DiscountCoupon> _discountCouponRepository;
    private readonly IDiscountProviderLoader _discountProviderLoader;
    private readonly IMediator _mediator;

    public DiscountValidationService(
        IDiscountProviderLoader discountProviderLoader,
        IRepository<DiscountCoupon> discountCouponRepository,
        IMediator mediator)
    {
        _discountCouponRepository = discountCouponRepository;
        _mediator = mediator;
        _discountProviderLoader = discountProviderLoader;
    }

    /// <summary>
    ///     Validate discount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <returns>Discount validation result</returns>
    public virtual async Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer,
        Store store,
        Currency currency)
    {
        ArgumentNullException.ThrowIfNull(discount);

        string[] couponCodesToValidate = null;
        if (customer != null)
            couponCodesToValidate = customer.ParseAppliedCouponCodes(SystemCustomerFieldNames.DiscountCoupons);

        return await ValidateDiscount(discount, customer, store, currency, couponCodesToValidate);
    }

    /// <summary>
    ///     Validate discount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="couponCodeToValidate">Coupon code</param>
    /// <returns>Discount validation result</returns>
    public virtual Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer, Store store,
        Currency currency, string couponCodeToValidate)
    {
        var couponCodes = string.IsNullOrWhiteSpace(couponCodeToValidate)
            ? Array.Empty<string>()
            : [
                couponCodeToValidate
            ];
        return ValidateDiscount(discount, customer, store, currency, couponCodes);
    }

    /// <summary>
    ///     Validate discount
    /// </summary>
    /// <param name="discount">Discount</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="couponCodesToValidate">Coupon codes</param>
    /// <returns>Discount validation result</returns>
    public virtual async Task<DiscountValidationResult> ValidateDiscount(Discount discount, Customer customer,
        Store store,
        Currency currency, string[] couponCodesToValidate)
    {
        ArgumentNullException.ThrowIfNull(discount);
        ArgumentNullException.ThrowIfNull(customer);

        var result = new DiscountValidationResult();

        //is enabled and use the same currency
        if (!discount.IsEnabled || discount.CurrencyCode != currency.CurrencyCode)
            return result;

        //time range check
        var now = DateTime.UtcNow;
        if (discount.StartDateUtc.HasValue)
        {
            var startDate = DateTime.SpecifyKind(discount.StartDateUtc.Value, DateTimeKind.Utc);
            if (startDate.CompareTo(now) > 0)
            {
                result.UserErrorResource = "ShoppingCart.Discount.NotStartedYet";
                return result;
            }
        }

        if (discount.EndDateUtc.HasValue)
        {
            var endDate = DateTime.SpecifyKind(discount.EndDateUtc.Value, DateTimeKind.Utc);
            if (endDate.CompareTo(now) < 0)
            {
                result.UserErrorResource = "ShoppingCart.Discount.Expired";
                return result;
            }
        }

        //do not allow use discount in the current store
        if (discount.LimitedToStores && discount.Stores.All(x => store.Id != x))
        {
            result.UserErrorResource = "ShoppingCart.Discount.CannotBeUsedInStore";
            return result;
        }

        //check coupon code
        if (discount.RequiresCouponCode)
        {
            if (couponCodesToValidate == null || couponCodesToValidate.Length == 0)
                return result;
            var exists = false;
            foreach (var item in couponCodesToValidate)
                if (discount.Reused)
                {
                    if (!await ExistsCodeInDiscount(item, discount.Id, null)) continue;
                    result.CouponCode = item;
                    exists = true;
                }
                else
                {
                    if (!await ExistsCodeInDiscount(item, discount.Id, false)) continue;
                    result.CouponCode = item;
                    exists = true;
                }

            if (!exists)
                return result;
        }

        if (discount.DiscountTypeId is DiscountType.AssignedToOrderSubTotal or DiscountType.AssignedToOrderTotal)
        {
            var cart = customer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                .ToList();

            var hasGiftVouchers = cart.Any(x => x.IsGiftVoucher);
            if (hasGiftVouchers)
            {
                result.UserErrorResource = "ShoppingCart.Discount.CannotBeUsedWithGiftVouchers";
                return result;
            }
        }

        //discount limitation - n times and n times per user
        switch (discount.DiscountLimitationId)
        {
            case DiscountLimitationType.NTimes:
            {
                var usedTimes = await _mediator.Send(new GetDiscountUsageHistoryQuery
                    { DiscountId = discount.Id, PageSize = 1 });
                if (usedTimes.TotalCount >= discount.LimitationTimes)
                    return result;
            }
                break;
            case DiscountLimitationType.NTimesPerUser:
            {
                var usedTimes = await _mediator.Send(new GetDiscountUsageHistoryQuery
                    { DiscountId = discount.Id, CustomerId = customer.Id, PageSize = 1 });
                if (usedTimes.TotalCount >= discount.LimitationTimes)
                {
                    result.UserErrorResource = "ShoppingCart.Discount.CannotBeUsedAnymore";
                    return result;
                }
            }
                break;
            case DiscountLimitationType.Nolimits:
            default:
                break;
        }

        //discount requirements
        var discountRules = discount.DiscountRules.ToList();
        foreach (var rule in discountRules)
        {
            //load a plugin
            var discountRequirementPlugin =
                _discountProviderLoader.LoadDiscountProviderByRuleSystemName(rule.DiscountRequirementRuleSystemName);

            if (discountRequirementPlugin == null)
                continue;

            if (!discountRequirementPlugin.IsAuthenticateStore(store))
                continue;

            var ruleRequest = new DiscountRuleValidationRequest {
                DiscountRule = rule,
                Discount = discount,
                Customer = customer,
                Store = store
            };
            var singleRequirementRule = discountRequirementPlugin.GetRequirementRules().FirstOrDefault(x =>
                x.SystemName.Equals(rule.DiscountRequirementRuleSystemName, StringComparison.OrdinalIgnoreCase));
            if (singleRequirementRule == null) return result;
            var ruleResult = await singleRequirementRule.CheckRequirement(ruleRequest);
            if (ruleResult.IsValid) continue;
            result.UserErrorResource = ruleResult.UserError;

            return result;
        }

        result.IsValid = true;
        return result;
    }

    /// <summary>
    ///     Exist coupon code in discount
    /// </summary>
    /// <param name="couponCode"></param>
    /// <param name="discountId"></param>
    /// <param name="used"></param>
    /// <returns></returns>
    public async Task<bool> ExistsCodeInDiscount(string couponCode, string discountId, bool? used)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            return false;

        var query = _discountCouponRepository.Table.Where(x => x.CouponCode == couponCode
                                                               && x.DiscountId == discountId);

        if (used.HasValue)
            query = query.Where(x => x.Used == used.Value);

        var result = await Task.FromResult(query.ToList());
        return result.Count != 0;
    }
}