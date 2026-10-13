using Grand.Business.Core.Interfaces.Authentication;

namespace Grand.Business.Core.Utilities.Authentication;

/// <summary>
///     Whether an account may use its GrandNode password; when not, the provider it must sign in with (null when that provider
///     is not available: sign-in is refused, never re-opened to the password). BreakGlass marks a password allowed only as the
///     emergency access of a provider-only installation
/// </summary>
public sealed record PasswordSignInDecision(bool Allowed, IExternalAuthenticationProvider Provider, bool BreakGlass = false)
{
    public static readonly PasswordSignInDecision Password = new(true, null);
}
