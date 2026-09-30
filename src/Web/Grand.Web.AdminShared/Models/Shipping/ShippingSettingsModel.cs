using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Models.Shipping;

public class ShippingSettingsModel : BaseModel
{
    public string ActiveStore { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.AllowPickUpInStore")]
    public bool AllowPickUpInStore { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.FreeShippingOverXEnabled")]
    public bool FreeShippingOverXEnabled { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.FreeShippingOverXValue")]
    public double FreeShippingOverXValue { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.FreeShippingOverXIncludingTax")]
    public bool FreeShippingOverXIncludingTax { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.EstimateShippingEnabled")]
    public bool EstimateShippingEnabled { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.DisplayShipmentEventsToCustomers")]
    public bool DisplayShipmentEventsToCustomers { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.DisplayShipmentEventsToStoreOwner")]
    public bool DisplayShipmentEventsToStoreOwner { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.SkipShippingMethodSelectionIfOnlyOne")]
    public bool SkipShippingMethodSelectionIfOnlyOne { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.AdditionalShippingChargeByQty")]
    public bool AdditionalShippingChargeByQty { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.DefaultShippingRate")]
    public double? DefaultShippingRate { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.HandlingTimeMinDays")]
    public int? HandlingTimeMinDays { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.HandlingTimeMaxDays")]
    public int? HandlingTimeMaxDays { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.TransitTimeMinDays")]
    public int? TransitTimeMinDays { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.TransitTimeMaxDays")]
    public int? TransitTimeMaxDays { get; set; }

    [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.ShippingOriginAddress")]
    public AddressModel ShippingOriginAddress { get; set; }
}