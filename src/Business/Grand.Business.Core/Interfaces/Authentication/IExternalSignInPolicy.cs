using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Accounts that must sign in with an external authentication provider instead of their password
/// </summary>
public interface IExternalSignInPolicy
{
    /// <summary>Whether the account may sign in (or recover its password) with its GrandNode password on the surface</summary>
    Task<PasswordSignInDecision> PasswordSignIn(Customer customer, SignInSurface surface);
}
