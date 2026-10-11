using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Authentication;

/// <summary>
///     Outcome of mapping a validated access token to a customer
/// </summary>
public class OidcCustomerResult
{
    private OidcCustomerResult(Customer customer, OidcFailureKind? failure, string description)
    {
        Customer = customer;
        Failure = failure;
        Description = description;
    }

    public Customer Customer { get; }
    public OidcFailureKind? Failure { get; }

    /// <summary>Fixed, token-independent text safe for error_description</summary>
    public string Description { get; }

    public bool Succeeded => Customer != null;

    public static OidcCustomerResult Success(Customer customer) => new(customer, null, null);
    public static OidcCustomerResult InvalidToken(string description) => new(null, OidcFailureKind.InvalidToken, description);
    public static OidcCustomerResult Forbidden(string description) => new(null, OidcFailureKind.Forbidden, description);
}
