using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     The stored links between customers and identities at the OpenID Connect provider: whether they belong to the
///     configured provider, and their removal
/// </summary>
public interface IOidcLinkService
{
    /// <summary>
    ///     False when OidcResourceServer:Authority differs from the provider the links were made with, or links exist and
    ///     their provider was never recorded: no link may then be used or made until an administrator removes or keeps
    ///     them. Without any link the configured provider is adopted
    /// </summary>
    Task<bool> LinksMatchProvider();

    /// <summary>The configured provider, the provider of the links and how many links exist</summary>
    Task<OidcLinkState> GetState();

    /// <summary>Removes every OpenID Connect link, ends the open provider sessions and adopts the configured provider</summary>
    Task<long> RemoveAll();

    /// <summary>Keeps the links for the configured provider (the same provider at a new address)</summary>
    Task KeepAll();

    /// <summary>Removes one external link of the customer; an OpenID Connect link also ends the customer's provider sessions</summary>
    Task<bool> Remove(Customer customer, string linkId);
}

/// <param name="ConfiguredAuthority">OidcResourceServer:Authority</param>
/// <param name="LinkedAuthority">The provider the links were made with (empty before the first use)</param>
/// <param name="LinkCount">Stored OpenID Connect links</param>
public sealed record OidcLinkState(string ConfiguredAuthority, string LinkedAuthority, long LinkCount)
{
    /// <summary>Links exist and their provider differs from the configured one, or was never recorded</summary>
    public bool ProviderChanged => LinkCount > 0 &&
                                   (ProviderUnknown || !OidcAuthority.SameProvider(LinkedAuthority, ConfiguredAuthority));

    /// <summary>Links exist that were made before their provider was recorded</summary>
    public bool ProviderUnknown => LinkCount > 0 && string.IsNullOrEmpty(LinkedAuthority);
}

public static class OidcAuthority
{
    /// <summary>Authorities compared without a trailing slash and without regard to case</summary>
    public static bool SameProvider(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string authority) => (authority ?? "").Trim().TrimEnd('/');
}
