using Grand.Business.Core.Interfaces.Authentication;
using Grand.Data;
using Grand.Domain.Customers;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Keeps the revocation time on the subject's links (one per store with per-store identity). Not cached: the check
///     must hold at once on every instance, and the lookup uses the provider + identifier index
/// </summary>
public class OidcTokenRevocationService(IRepository<ExternalAuthentication> externalAuthenticationRepository)
    : IOidcTokenRevocationService
{
    public Task RevokeTokens(string subject)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);
        return externalAuthenticationRepository.UpdateManyAsync(
            x => x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName && x.ExternalIdentifier == subject,
            UpdateBuilder<ExternalAuthentication>.Create().Set(x => x.TokensRevokedBeforeUtc, DateTime.UtcNow));
    }

    public async Task<bool> IsRevoked(string subject, DateTime issuedUtc)
    {
        if (string.IsNullOrEmpty(subject))
            return false;
        return await externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName && x.ExternalIdentifier == subject &&
            x.TokensRevokedBeforeUtc != null && x.TokensRevokedBeforeUtc > issuedUtc) != null;
    }
}
