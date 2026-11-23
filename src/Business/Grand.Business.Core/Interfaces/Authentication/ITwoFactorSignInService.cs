using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

public interface ITwoFactorSignInService
{
    /// <summary>
    ///     Checks the code of a sign-in waiting for its second step. Checks of one customer run one at a time, a locked-out
    ///     customer is refused, and wrong codes are counted on the customer, so a new password step does not start the
    ///     count again: the last one allowed locks the customer out.
    /// </summary>
    Task<TwoFactorSignInResult> CheckCode(Customer customer, string token,
        TwoFactorAuthenticationType twoFactorAuthenticationType);
}
