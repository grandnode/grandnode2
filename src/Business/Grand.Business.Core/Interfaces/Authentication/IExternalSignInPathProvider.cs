namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     An external authentication provider that signs in through its own page: offered on the panels' login pages, and
///     used to send the browser through the provider once more after a registration that connected the identity
/// </summary>
public interface IExternalSignInPathProvider : IExternalAuthenticationProvider
{
    /// <summary>
    ///     Path (from the application root, starting with "/") that signs in and returns to <paramref name="returnUrl" />;
    ///     null when the provider cannot sign in now (for example, not configured)
    /// </summary>
    Task<string> GetSignInPath(string returnUrl);
}
