using Grand.Domain.Customers;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Settings
{
    public partial class CustomerSettingsModel : BaseModel
    {
        public CustomerSettingsModel()
        {
            CustomerSettings = new CustomersSettingsModel();
            AddressSettings = new AddressSettingsModel();
        }
        public CustomersSettingsModel CustomerSettings { get; set; }
        public AddressSettingsModel AddressSettings { get; set; }

        #region Nested classes

        public partial class CustomersSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.Customer.FirstLastNameRequired")]
            public bool FirstLastNameRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AllowUsersToChangeEmail")]
            public bool AllowUsersToChangeEmail { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.UsernamesEnabled")]
            public bool UsernamesEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AllowUsersToChangeUsernames")]
            public bool AllowUsersToChangeUsernames { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.CheckUsernameAvailabilityEnabled")]
            public bool CheckUsernameAvailabilityEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.UserRegistrationType")]
            public int UserRegistrationType { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.GeoEnabled")]
            public bool GeoEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.NotifyNewCustomerRegistration")]
            public bool NotifyNewCustomerRegistration { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideDownloadableProductsTab")]
            public bool HideDownloadableProductsTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideOutOfStockSubscriptionsTab")]
            public bool HideOutOfStockSubscriptionsTab { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.HideAuctionsTab")]
            public bool HideAuctionsTab { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.HideNotesTab")]
            public bool HideNotesTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AllowUsersToDeleteAccount")]
            public bool AllowUsersToDeleteAccount { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AllowUsersToExportData")]
            public bool AllowUsersToExportData { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.CustomerNameFormat")]
            public int CustomerNameFormat { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.PasswordRegularExpression")]
            public string PasswordRegularExpression { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.UnduplicatedPasswordsNumber")]
            public int UnduplicatedPasswordsNumber { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.PasswordRecoveryLinkDaysValid")]
            public int PasswordRecoveryLinkDaysValid { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.PasswordLifetime")]
            public int PasswordLifetime { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.DefaultPasswordFormat")]
            public int DefaultPasswordFormat { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.FailedPasswordAllowedAttempts")]
            public int FailedPasswordAllowedAttempts { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.FailedPasswordLockoutMinutes")]
            public int FailedPasswordLockoutMinutes { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.NewsletterEnabled")]
            public bool NewsletterEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.NewsletterTickedByDefault")]
            public bool NewsletterTickedByDefault { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideNewsletterBlock")]
            public bool HideNewsletterBlock { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.NewsletterBlockAllowToUnsubscribe")]
            public bool NewsletterBlockAllowToUnsubscribe { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.RegistrationFreeShipping")]
            public bool RegistrationFreeShipping { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.StoreLastVisitedPage")]
            public bool StoreLastVisitedPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.GenderEnabled")]
            public bool GenderEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.DateOfBirthEnabled")]
            public bool DateOfBirthEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.DateOfBirthRequired")]
            public bool DateOfBirthRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.DateOfBirthMinimumAge")]
            [UIHint("Int32Nullable")]
            public int? DateOfBirthMinimumAge { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.CompanyEnabled")]
            public bool CompanyEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.CompanyRequired")]
            public bool CompanyRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.StreetAddressEnabled")]
            public bool StreetAddressEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.StreetAddressRequired")]
            public bool StreetAddressRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.StreetAddress2Enabled")]
            public bool StreetAddress2Enabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.StreetAddress2Required")]
            public bool StreetAddress2Required { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.ZipPostalCodeEnabled")]
            public bool ZipPostalCodeEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.ZipPostalCodeRequired")]
            public bool ZipPostalCodeRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.CityEnabled")]
            public bool CityEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.CityRequired")]
            public bool CityRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.CountryEnabled")]
            public bool CountryEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.CountryRequired")]
            public bool CountryRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.StateProvinceEnabled")]
            public bool StateProvinceEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.StateProvinceRequired")]
            public bool StateProvinceRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.PhoneEnabled")]
            public bool PhoneEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.PhoneRequired")]
            public bool PhoneRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.FaxEnabled")]
            public bool FaxEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.FaxRequired")]
            public bool FaxRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AcceptPrivacyPolicyEnabled")]
            public bool AcceptPrivacyPolicyEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideReviewsTab")]
            public bool HideReviewsTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideCoursesTab")]
            public bool HideCoursesTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.TwoFactorAuthenticationEnabled")]
            public bool TwoFactorAuthenticationEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.TwoFactorAuthenticationType")]
            public int TwoFactorAuthenticationType { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideSubaccountsTab")]
            public bool HideSubaccountsTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HideDocumentsTab")]
            public bool HideDocumentsTab { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.SaveVisitedPage")]
            public bool SaveVisitedPage { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.OnlineCustomerMinutes")]
            public int OnlineCustomerMinutes { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.OnlineShoppingCartMinutes")]
            public int OnlineShoppingCartMinutes { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.HashedPasswordFormat")]
            public HashedPasswordFormat HashedPasswordFormat { get; set; }
        }

        public partial class AddressSettingsModel : BaseModel
        {
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.AddressTypeEnabled")]
            public bool AddressTypeEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.CompanyEnabled")]
            public bool CompanyEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.CompanyRequired")]
            public bool CompanyRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.VatNumberEnabled")]
            public bool VatNumberEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.VatNumberRequired")]
            public bool VatNumberRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.StreetAddressEnabled")]
            public bool StreetAddressEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.StreetAddressRequired")]
            public bool StreetAddressRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.StreetAddress2Enabled")]
            public bool StreetAddress2Enabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.StreetAddress2Required")]
            public bool StreetAddress2Required { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.ZipPostalCodeEnabled")]
            public bool ZipPostalCodeEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.ZipPostalCodeRequired")]
            public bool ZipPostalCodeRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.CityEnabled")]
            public bool CityEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.CityRequired")]
            public bool CityRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.CountryEnabled")]
            public bool CountryEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.StateProvinceEnabled")]
            public bool StateProvinceEnabled { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.PhoneEnabled")]
            public bool PhoneEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.PhoneRequired")]
            public bool PhoneRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.FaxEnabled")]
            public bool FaxEnabled { get; set; }
            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.FaxRequired")]
            public bool FaxRequired { get; set; }

            [GrandResourceDisplayName("Admin.Settings.Customer.AddressFormFields.NoteEnabled")]
            public bool NoteEnabled { get; set; }
        }


        #endregion
    }
}