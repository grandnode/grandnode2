using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;

namespace Grand.Business.Core.Utilities.Checkout;

public class PlaceOrderContainer
{
    /// <summary>
    ///     Customer, store, working currency and tax display the order totals are calculated for
    /// </summary>
    public CalculationContext Context { get; set; }

    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public Language Language { get; set; }

    /// <summary>
    ///     Language of the request placing the order, used for the attribute descriptions
    /// </summary>
    public Language WorkingLanguage { get; set; }

    /// <summary>
    ///     Store address used for file download links in the attribute descriptions
    /// </summary>
    public DomainHost Host { get; set; }

    public Currency Currency { get; set; }
    public string AffiliateId { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public double CurrencyRate { get; set; }
    public string PrimaryCurrencyCode { get; set; }
    public string PaymentMethodSystemName { get; set; }
    public string PurchaseOrderNumber { get; set; }

    /// <summary>
    ///     An order placement rule requires an approval, so the order is placed awaiting one
    /// </summary>
    public bool ApprovalRequired { get; set; }

    /// <summary>
    ///     Why the order needs an approval
    /// </summary>
    public IList<string> ApprovalReasons { get; set; } = new List<string>();

    /// <summary>
    ///     A rule wants the store to decide the approval, also for a sub-account's order
    /// </summary>
    public bool ApprovalStoreDecides { get; set; }

    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
    public ShippingStatus ShippingStatus { get; set; }
    public string ShippingMethodName { get; set; }
    public string ShippingRateProviderSystemName { get; set; }
    public bool PickUpInStore { get; set; }
    public PickupPoint PickupPoint { get; set; }
    public string CheckoutAttributeDescription { get; set; }
    public IList<CustomAttribute> CheckoutAttributes { get; set; } = new List<CustomAttribute>();
    public IList<ShoppingCartItem> Cart { get; set; } = new List<ShoppingCartItem>();
    public IList<OrderTax> Taxes { get; set; } = new List<OrderTax>();
    public List<ApplyDiscount> AppliedDiscounts { get; set; } = new();
    public List<AppliedGiftVoucher> AppliedGiftVouchers { get; set; } = new();
    public bool IsRecurring { get; set; }
    public int RecurringCycleLength { get; set; }
    public RecurringCyclePeriod RecurringCyclePeriodId { get; set; }
    public int RecurringTotalCycles { get; set; }
    public double OrderSubTotalInclTax { get; set; }
    public double OrderSubTotalExclTax { get; set; }
    public double OrderSubTotalDiscountInclTax { get; set; }
    public double OrderSubTotalDiscountExclTax { get; set; }
    public double OrderShippingTotalInclTax { get; set; }
    public double OrderShippingTotalExclTax { get; set; }
    public double PaymentAdditionalFeeInclTax { get; set; }
    public double PaymentAdditionalFeeExclTax { get; set; }
    public double OrderTaxTotal { get; set; }
    public double OrderDiscountAmount { get; set; }
    public int RedeemedLoyaltyPoints { get; set; }
    public double RedeemedLoyaltyPointsAmount { get; set; }
    public double OrderTotal { get; set; }
}