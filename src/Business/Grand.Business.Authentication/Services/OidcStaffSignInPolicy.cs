using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     OidcLogin:StaffSsoOnly - a back-office account connected to the identity provider with its password (a link not made
///     by e-mail) signs in only through the provider, while the OpenID Connect login is installed, active and configured.
///     Accounts without such a link keep the password: the break-glass account, and accounts still to be connected
/// </summary>
public class OidcStaffSignInPolicy(
    IRepository<ExternalAuthentication> externalAuthenticationRepository,
    IPermissionService permissionService,
    IEnumerable<IExternalAuthenticationProvider> externalAuthenticationProviders,
    ExternalAuthenticationSettings externalAuthenticationSettings,
    OidcLoginConfig config,
    OidcResourceServerConfig resourceServerConfig) : IExternalSignInPolicy
{
    public async Task<IExternalAuthenticationProvider> RequiredProvider(Customer customer)
    {
        if (customer == null || !config.StaffSsoOnly || !config.IsConfigured(resourceServerConfig))
            return null;

        var provider = externalAuthenticationProviders.FirstOrDefault(x =>
            x.SystemName.Equals(OidcCustomerResolver.ProviderSystemName, StringComparison.OrdinalIgnoreCase));
        if (provider == null || !provider.IsMethodActive(externalAuthenticationSettings))
            return null;

        if (!await permissionService.IsBackOfficeAccount(customer))
            return null;

        var confirmed = await externalAuthenticationRepository.GetOneAsync(x =>
            x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName && x.CustomerId == customer.Id &&
            !x.LinkedByEmail);
        return confirmed != null ? provider : null;
    }
}
