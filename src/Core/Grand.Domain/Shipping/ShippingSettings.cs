using Grand.Domain.Common;
using Grand.Domain.Configuration;

namespace Grand.Domain.Shipping;

public class ShippingSettings : ISettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether customers can choose "Pick Up in Store" option during checkout
    /// </summary>
    public bool AllowPickUpInStore { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether 'Free shipping over X' is enabled
    /// </summary>
    public bool FreeShippingOverXEnabled { get; set; }

    /// <summary>
    ///     Gets or sets a value of 'Free shipping over X' option
    /// </summary>
    public double FreeShippingOverXValue { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether 'Free shipping over X' option
    ///     should be evaluated over 'X' value including tax or not
    /// </summary>
    public bool FreeShippingOverXIncludingTax { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether 'Estimate shipping' option is enabled
    /// </summary>
    public bool EstimateShippingEnabled { get; set; }

    /// <summary>
    ///     A value indicating whether customers should see shipment events on their order details pages
    /// </summary>
    public bool DisplayShipmentEventsToCustomers { get; set; }

    /// <summary>
    ///     A value indicating whether store owner should see shipment events on the shipment details pages
    /// </summary>
    public bool DisplayShipmentEventsToStoreOwner { get; set; }

    /// <summary>
    ///     Gets or sets shipping origin address
    /// </summary>
    public Address ShippingOriginAddress { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether we should skip 'select shipping method' page if we have only one shipping
    ///     method
    /// </summary>
    public bool SkipShippingMethodSelectionIfOnlyOne { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether we should calculate additional shipping charge on product multiply by the
    ///     quantity
    /// </summary>
    public bool AdditionalShippingChargeByQty { get; set; }

    /// <summary>
    ///     Gets or sets the standard cost of shipping one item, in the primary store currency, entered like a
    ///     shipping method's rate. Optional - informational only, published in structured data
    ///     (OfferShippingDetails.shippingRate) for products without free shipping; checkout never uses it,
    ///     the rate there always comes from the shipping provider
    /// </summary>
    public double? DefaultShippingRate { get; set; }

    /// <summary>
    ///     Gets or sets the minimum number of business days between the order and handing it to the carrier.
    ///     Optional - informational only, published in structured data (OfferShippingDetails.deliveryTime)
    /// </summary>
    public int? HandlingTimeMinDays { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of business days between the order and handing it to the carrier
    /// </summary>
    public int? HandlingTimeMaxDays { get; set; }

    /// <summary>
    ///     Gets or sets the minimum number of business days the carrier needs to deliver.
    ///     Optional - informational only, published in structured data (OfferShippingDetails.deliveryTime)
    /// </summary>
    public int? TransitTimeMinDays { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of business days the carrier needs to deliver
    /// </summary>
    public int? TransitTimeMaxDays { get; set; }
}