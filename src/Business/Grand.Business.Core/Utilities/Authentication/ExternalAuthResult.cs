using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Authentication;

public enum ExternalAuthStatus
{
    /// <summary>An existing customer associated with the external account is to be signed in</summary>
    LoggedIn,

    /// <summary>A new customer was registered, approved and is to be signed in</summary>
    Registered,

    /// <summary>The external account is associated with the customer already signed in; nothing to sign in</summary>
    Associated,

    /// <summary>A new customer was registered and waits for admin approval</summary>
    AwaitingApproval,

    /// <summary>The external account belongs to another customer than the one signed in</summary>
    AlreadyAssigned,

    RegistrationDisabled,
    Error
}

/// <summary>
///     Outcome of an external authentication; signing in and redirecting is left to the caller
/// </summary>
public class ExternalAuthResult
{
    private ExternalAuthResult(ExternalAuthStatus status, Customer customer, IList<string> errors)
    {
        Status = status;
        Customer = customer;
        Errors = errors;
    }

    public ExternalAuthStatus Status { get; }

    /// <summary>The customer to sign in (LoggedIn, Registered) or the one the account was associated with</summary>
    public Customer Customer { get; }

    public IList<string> Errors { get; }

    public static ExternalAuthResult LoggedIn(Customer customer) => new(ExternalAuthStatus.LoggedIn, customer, []);
    public static ExternalAuthResult Registered(Customer customer) => new(ExternalAuthStatus.Registered, customer, []);
    public static ExternalAuthResult Associated(Customer customer) => new(ExternalAuthStatus.Associated, customer, []);

    public static ExternalAuthResult AwaitingApproval(Customer customer) =>
        new(ExternalAuthStatus.AwaitingApproval, customer, []);

    public static ExternalAuthResult AlreadyAssigned() =>
        new(ExternalAuthStatus.AlreadyAssigned, null, ["Account is already assigned"]);

    public static ExternalAuthResult RegistrationDisabled() =>
        new(ExternalAuthStatus.RegistrationDisabled, null, ["Registration is disabled"]);

    public static ExternalAuthResult Error(string error) => new(ExternalAuthStatus.Error, null, [error]);
}
