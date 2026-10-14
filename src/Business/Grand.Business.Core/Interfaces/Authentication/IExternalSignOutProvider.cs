namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>An external authentication provider that can end the user's session at the provider (RP-initiated logout)</summary>
public interface IExternalSignOutProvider
{
    /// <summary>
    ///     Absolute URL that ends the session at the provider and returns to <paramref name="postLogoutRedirectUri" />
    ///     (registered at the provider); null when the provider cannot do it now
    /// </summary>
    Task<string> GetSignOutUrl(string postLogoutRedirectUri);
}
