namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>Where a password sign-in (or recovery) is attempted; break-glass accounts keep the password only on the Admin panel</summary>
public enum SignInSurface
{
    Storefront,
    AdminPanel,
    StorePanel,
    VendorPanel,
    FrontendApi
}
