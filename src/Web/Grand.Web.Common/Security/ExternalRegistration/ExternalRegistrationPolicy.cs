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
}
