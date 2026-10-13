using Grand.Business.Core.Interfaces.Authentication;

namespace Grand.Web.AdminShared.Extensions;

public static class PanelSignInSurface
{
    /// <summary>The panel a login or password recovery belongs to; an unknown area never counts as the Admin panel</summary>
    public static SignInSurface FromArea(string area) => area?.ToLowerInvariant() switch {
        "admin" => SignInSurface.AdminPanel,
        "store" => SignInSurface.StorePanel,
        "vendor" => SignInSurface.VendorPanel,
        _ => SignInSurface.Storefront
    };
}
