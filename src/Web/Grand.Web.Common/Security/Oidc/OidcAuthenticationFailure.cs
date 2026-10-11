using Grand.Business.Core.Utilities.Authentication;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Why the Oidc scheme rejected the current request; the challenge reads it to choose status and header
/// </summary>
public class OidcAuthenticationFailure
{
    public OidcFailureKind Kind { get; init; }

    /// <summary>Fixed, token-independent text</summary>
    public string Description { get; init; }

    /// <summary>Space-separated scopes for insufficient_scope</summary>
    public string Scope { get; init; }

    public static void Set(HttpContext context, OidcAuthenticationFailure failure) =>
        context.Items[OidcResourceServerDefaults.FailureItemKey] = failure;

    public static OidcAuthenticationFailure Get(HttpContext context) =>
        context.Items.TryGetValue(OidcResourceServerDefaults.FailureItemKey, out var value)
            ? value as OidcAuthenticationFailure
            : null;
}
