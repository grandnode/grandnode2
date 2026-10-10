namespace Grand.Web.Common.Security.ExternalRegistration;

/// <summary>
///     Identity confirmed by an external provider that is not yet connected to a customer: kept in the external
///     authentication cookie while the user registers or connects an existing account
/// </summary>
public record ExternalRegistrationIdentity(
    string ProviderSystemName,
    string Subject,
    string Email,
    bool EmailVerified,
    string FirstName,
    string LastName);
