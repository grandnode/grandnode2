using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class VendorSettingsModel : BaseModel
    {

        public VendorSettingsModel()
        {
            AddressSettings = new AddressSettingsModel();
        }

        public string ActiveStore { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.VendorsBlockItemsToDisplay")]
        public int VendorsBlockItemsToDisplay { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.ShowVendorOnProductDetailsPage")]
        public bool ShowVendorOnProductDetailsPage { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.AllowCustomersToContactVendors")]
        public bool AllowCustomersToContactVendors { get; set; }
        [GrandResourceDisplayName("Admin.Settings.Vendor.AllowCustomersToApplyForVendorAccount")]
        public bool AllowCustomersToApplyForVendorAccount { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.AllowSearchByVendor")]
        public bool AllowSearchByVendor { get; set; }


        [GrandResourceDisplayName("Admin.Settings.Vendor.AllowVendorsToEditInfo")]
        public bool AllowVendorsToEditInfo { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.NotifyStoreOwnerAboutVendorInformationChange")]
        public bool NotifyStoreOwnerAboutVendorInformationChange { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.TermsOfServiceEnabled")]
        public bool TermsOfServiceEnabled { get; set; }

        //review vendor
        [GrandResourceDisplayName("Admin.Settings.Vendor.VendorReviewsMustBeApproved")]
        public bool VendorReviewsMustBeApproved { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.AllowAnonymousUsersToReviewVendor")]
        public bool AllowAnonymousUsersToReviewVendor { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.DefaultAdminTheme")]
        public string DefaultAdminTheme { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.VendorReviewPossibleOnlyAfterPurchasing")]
        public bool VendorReviewPossibleOnlyAfterPurchasing { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.NotifyVendorAboutNewVendorReviews")]
        public bool NotifyVendorAboutNewVendorReviews { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.DefaultAllowCustomerReview")]
        public bool DefaultAllowCustomerReview { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.DefaultVendorRatingValue")]
        public int DefaultVendorRatingValue { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Vendor.DefaultVendorPageSizeOptions")]
        public string DefaultVendorPageSizeOptions { get; set; }

        public AddressSettingsModel AddressSettings { get; set; }

        #region Nested classes

        public partial class AddressSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.CompanyEnabled")]
            public bool CompanyEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.CompanyRequired")]
            public bool CompanyRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.StreetAddressEnabled")]
            public bool StreetAddressEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.StreetAddressRequired")]
            public bool StreetAddressRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.StreetAddress2Enabled")]
            public bool StreetAddress2Enabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.StreetAddress2Required")]
            public bool StreetAddress2Required { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.ZipPostalCodeEnabled")]
            public bool ZipPostalCodeEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.ZipPostalCodeRequired")]
            public bool ZipPostalCodeRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.CityEnabled")]
            public bool CityEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.CityRequired")]
            public bool CityRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.CountryEnabled")]
            public bool CountryEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.StateProvinceEnabled")]
            public bool StateProvinceEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.PhoneEnabled")]
            public bool PhoneEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.PhoneRequired")]
            public bool PhoneRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.FaxEnabled")]
            public bool FaxEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Vendor.AddressFormFields.FaxRequired")]
            public bool FaxRequired { get; set; }
        }

        #endregion

    }



}