using Grand.Domain.Customers;

namespace Grand.Web.Common.Security.ExternalRegistration;

public static class ExternalRegistrationPolicy
{
    /// <summary>
    ///     Registration type for an account created from a provider identity: a verified e-mail needs no second
    ///     validation, an unverified one always does (or an administrator's approval, which the store asked for)
    /// </summary>
    public static UserRegistrationType EffectiveRegistrationType(UserRegistrationType storeType, bool emailVerified) =>
        storeType switch {
            UserRegistrationType.EmailValidation when emailVerified => UserRegistrationType.Standard,
            UserRegistrationType.Standard when !emailVerified => UserRegistrationType.EmailValidation,
            _ => storeType
        };

    /// <summary>
    ///     Whether registering connects the provider identity to the new account and needs no password: only for a
    ///     verified e-mail. An unverified one registers with a password like any other account and connects later with
    ///     that password - otherwise whoever holds the identity would keep the account once the owner of the address
    ///     activates it, even after a password reset.
    /// </summary>
    public static bool ConnectsAtRegistration(ExternalRegistrationIdentity pending) => pending is { EmailVerified: true };
}
