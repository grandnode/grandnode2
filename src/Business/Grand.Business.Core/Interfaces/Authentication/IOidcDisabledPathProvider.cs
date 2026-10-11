namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Lets a module that can be switched off withdraw the paths it serves from OIDC protection while it is off. A
///     protected resource on, under or over one of these paths is left out of the registry: a token there is never
///     validated by the Oidc scheme, mapped to a customer or used to pin the store, and the resource is not published.
///     Read once, when the registry is built, so the switch must be a startup setting. Register the implementation
///     as a singleton.
/// </summary>
public interface IOidcDisabledPathProvider
{
    IEnumerable<string> GetDisabledPaths();
}
