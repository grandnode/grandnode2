namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Refuses identity provider access tokens of a subject issued before the provider ended the subject's session
///     or refused the account
/// </summary>
public interface IOidcTokenRevocationService
{
    /// <summary>Tokens of the subject issued until now are refused</summary>
    Task RevokeTokens(string subject);

    /// <summary>The token of the subject issued at <paramref name="issuedUtc" /> is refused</summary>
    Task<bool> IsRevoked(string subject, DateTime issuedUtc);

    /// <summary>Deletes revocations older than any access token they could still refuse</summary>
    Task DeleteExpired();
}
