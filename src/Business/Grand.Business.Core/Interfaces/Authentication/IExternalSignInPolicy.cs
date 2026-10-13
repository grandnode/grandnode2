using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Accounts that must sign in with an external authentication provider instead of their password
/// </summary>
public interface IExternalSignInPolicy
{
    /// <summary>The provider the account must sign in with; null when its password may be used</summary>
    Task<IExternalAuthenticationProvider> RequiredProvider(Customer customer);
}
