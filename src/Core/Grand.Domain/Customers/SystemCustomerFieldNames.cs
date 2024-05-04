namespace Grand.Domain.Customers;

public static class SystemCustomerFieldNames
{
    //Form fields
    public static string FirstName => "FirstName";
    public static string LastName => "LastName";
    public static string Gender => "Gender";
    public static string DateOfBirth => "DateOfBirth";
    public static string Company => "Company";
    public static string StreetAddress => "StreetAddress";
    public static string StreetAddress2 => "StreetAddress2";
    public static string ZipPostalCode => "ZipPostalCode";
    public static string City => "City";
    public static string CountryId => "CountryId";
    public static string StateProvinceId => "StateProvinceId";
    public static string Phone => "Phone";
    public static string Fax => "Fax";
    public static string VatNumber => "VatNumber";
    public static string VatNumberStatusId => "VatNumberStatusId";
    public static string PasswordToken => "PasswordToken";

    //Other attributes
    public static string DiscountCoupons => "DiscountCoupons";
    public static string GiftVoucherCoupons => "GiftVoucherCoupons";
    public static string UrlReferrer => "UrlReferrer";
    public static string PasswordRecoveryToken => "PasswordRecoveryToken";
    public static string PasswordRecoveryTokenDateGenerated => "PasswordRecoveryTokenDateGenerated";
    public static string AccountActivationToken => "AccountActivationToken";
    public static string LastVisitedPage => "LastVisitedPage";
    public static string LastUrlReferrer => "LastUrlReferrer";
    public static string ImpersonatedCustomerId => "ImpersonatedCustomerId";
    public static string AdminAreaStoreScopeConfiguration => "AdminAreaStoreScopeConfiguration";
    public static string TwoFactorEnabled => "TwoFactorEnabled";
    public static string TwoFactorSecretKey => "TwoFactorSecretKey";
    public static string TwoFactorValidCode => "TwoFactorValidCode";
    public static string TwoFactorCodeValidUntil => "TwoFactorValidCodeUntil";

    public static string CurrencyId => "CurrencyId";
    public static string LanguageId => "LanguageId";
    public static string SelectedPaymentMethod => "SelectedPaymentMethod";
    public static string PaymentTransaction => "PaymentTransaction";
    public static string PaymentOptionAttribute => "PaymentOptionAttribute";

    public static string SelectedShippingOption => "SelectedShippingOption";

    //value customer chose "pick up in store" option
    public static string SelectedPickupPoint => "SelectedPickupPoint";
    public static string CheckoutAttributes => "CheckoutAttributes";
    public static string OfferedShippingOptions => "OfferedShippingOptions";
    public static string ShippingOptionAttributeDescription => "ShippingOptionAttributeDescription";
    public static string ShippingOptionAttribute => "ShippingOptionAttribute";
    public static string LastContinueShoppingPage => "LastContinueShoppingPage";
    public static string TaxDisplayTypeId => "TaxDisplayTypeId";
    public static string UseLoyaltyPointsDuringCheckout => "UseLoyaltyPointsDuringCheckout";
    public static string CookieAccepted => "Cookie.Accepted";
    public static string ConsentCookies => "ConsentCookies";
    public static string RefreshToken => "RefreshToken";
}