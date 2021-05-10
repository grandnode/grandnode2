using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Tax
{
    public partial class TaxSettingsModel : BaseModel
    {
        public TaxSettingsModel()
        {
            TaxCategories = new List<SelectListItem>();
            EuVatShopCountries = new List<SelectListItem>();
            DefaultTaxAddress = new AddressModel();
        }

        public string ActiveStore { get; set; }


        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.PricesIncludeTax")]
        public bool PricesIncludeTax { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.AllowCustomersToSelectTaxDisplayType")]
        public bool AllowCustomersToSelectTaxDisplayType { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.TaxDisplayType")]
        public int TaxDisplayType { get; set; }
        public SelectList TaxDisplayTypeValues { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.DisplayTaxSuffix")]
        public bool DisplayTaxSuffix { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.DisplayTaxRates")]
        public bool DisplayTaxRates { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.HideZeroTax")]
        public bool HideZeroTax { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.HideTaxInOrderSummary")]
        public bool HideTaxInOrderSummary { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.ForceTaxExclusionFromOrderSubtotal")]
        public bool ForceTaxExclusionFromOrderSubtotal { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.TaxBasedOn")]
        public int TaxBasedOn { get; set; }
        public SelectList TaxBasedOnValues { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.DefaultTaxAddress")]
        public AddressModel DefaultTaxAddress { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.DefaultTaxCategory")]
        public string DefaultTaxCategoryId { get; set; }

        public IList<SelectListItem> TaxCategories { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.ShippingIsTaxable")]
        public bool ShippingIsTaxable { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.ShippingPriceIncludesTax")]
        public bool ShippingPriceIncludesTax { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.ShippingTaxCategory")]
        public string ShippingTaxCategoryId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.PaymentMethodAdditionalFeeIsTaxable")]
        public bool PaymentMethodAdditionalFeeIsTaxable { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.PaymentMethodAdditionalFeeIncludesTax")]
        public bool PaymentMethodAdditionalFeeIncludesTax { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.PaymentMethodAdditionalFeeTaxCategory")]
        public string PaymentMethodAdditionalFeeTaxCategoryId { get; set; }
        
        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.EuVatEnabled")]
        public bool EuVatEnabled { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.EuVatShopCountry")]
        public string EuVatShopCountryId { get; set; }
        public IList<SelectListItem> EuVatShopCountries { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.EuVatAllowVatExemption")]
        public bool EuVatAllowVatExemption { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.EuVatUseWebService")]
        public bool EuVatUseWebService { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.EuVatAssumeValid")]
        public bool EuVatAssumeValid { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.GetCountryByIPAddress")]
        public bool GetCountryByIPAddress { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.CalculateRoundPrice")]
        public int CalculateRoundPrice { get; set; } = 2;

        [GrandResourceDisplayName("Admin.Configuration.Tax.Settings.MidpointRounding")]
        public MidpointRounding MidpointRounding { get; set; }
    }
}