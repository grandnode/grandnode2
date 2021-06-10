using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Models.Shipping
{
    public partial class ShippingSettingsModel : BaseModel
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

        [GrandResourceDisplayName("Admin.Configuration.Shipping.Settings.ShippingOriginAddress")]
        public AddressModel ShippingOriginAddress { get; set; }
    }
}