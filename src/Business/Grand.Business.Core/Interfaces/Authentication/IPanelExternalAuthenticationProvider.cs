namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     An external authentication provider offered on the login pages of the Admin, Store and Vendor panels
/// </summary>
public interface IPanelExternalAuthenticationProvider : IExternalAuthenticationProvider
{
    /// <summary>
    ///     Path (from the application root, starting with "/") that signs in and returns to <paramref name="returnUrl" />;
    ///     null hides the button (for example, not configured)
    /// </summary>
    Task<string> GetPanelSignInPath(string returnUrl);
}
