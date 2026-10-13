using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Authentication;

public enum OidcLoginOutcome
{
    SignIn,
    RegistrationRequired,
    LinkRequired,
    Refused
}

/// <summary>
///     Why a browser sign-in was refused; the login page picks a fixed message by it
/// </summary>
public enum OidcLoginRefusal
{
    None,
    InvalidIdentity,
    AccountInactive,
    AccountUnusable,
    LinkedToOtherIdentity,
    RegistrationDisabled,
    EmailNotVerified
}

public enum OidcLinkResult
{
    Linked,
    SubjectLinkedToOtherCustomer,
    CustomerLinkedToOtherSubject
}

/// <summary>
///     Outcome of mapping a provider identity to a customer during a browser sign-in
/// </summary>
public class OidcLoginResult
{
    private OidcLoginResult(OidcLoginOutcome outcome, Customer customer, OidcLoginRefusal refusal)
    {
        Outcome = outcome;
        Customer = customer;
        Refusal = refusal;
    }

    public OidcLoginOutcome Outcome { get; }

    /// <summary>The customer to sign in (SignIn) or the account to connect (LinkRequired)</summary>
    public Customer Customer { get; }

    public OidcLoginRefusal Refusal { get; }

    public static OidcLoginResult SignIn(Customer customer) => new(OidcLoginOutcome.SignIn, customer, OidcLoginRefusal.None);
    public static OidcLoginResult RegistrationRequired() => new(OidcLoginOutcome.RegistrationRequired, null, OidcLoginRefusal.None);
    public static OidcLoginResult LinkRequired(Customer customer) => new(OidcLoginOutcome.LinkRequired, customer, OidcLoginRefusal.None);
    public static OidcLoginResult Refused(OidcLoginRefusal refusal) => new(OidcLoginOutcome.Refused, null, refusal);
}
