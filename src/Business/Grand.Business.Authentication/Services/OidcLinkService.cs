using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Ties the stored OpenID Connect links to the provider they were made with (OidcLinkSettings, store-independent). A
///     subject is unique only within one provider: after OidcResourceServer:Authority changes, a subject of the new provider
///     equal to a stored one would select another person's account, so no link is used or made until an administrator
///     removes the links (another provider) or keeps them (the same provider at a new address)
/// </summary>
public class OidcLinkService(
    ISettingService settingService,
    OidcResourceServerConfig config,
    IRepository<ExternalAuthentication> externalAuthenticationRepository,
    IOidcSessionService oidcSessionService,
    ILogger<OidcLinkService> logger) : IOidcLinkService
{
    //every refused request would log it otherwise (access tokens are checked on each request)
    private static long _lastMismatchLogTicks;

    //concurrent first requests of one instance record the provider once (each save also clears the cache)
    private static readonly SemaphoreSlim AdoptLock = new(1, 1);

    public async Task<bool> LinksMatchProvider()
    {
        var settings = await settingService.LoadSetting<OidcLinkSettings>();
        if (!string.IsNullOrWhiteSpace(settings?.Authority) && OidcAuthority.SameProvider(settings.Authority, config.Authority))
            return true;

        //no link a subject could select: the configured provider is adopted (first use, or a provider changed before
        //anyone connected). Links of an unrecorded provider are not adopted - the provider may have changed together
        //with the upgrade that started recording it
        if (!await HasLinks())
        {
            await Adopt();
            return true;
        }

        LogMismatch(settings?.Authority);
        return false;
    }

    public async Task<OidcLinkState> GetState()
    {
        var settings = await settingService.LoadSetting<OidcLinkSettings>();
        var count = await externalAuthenticationRepository.CountAsync(OidcLinks());
        return new OidcLinkState(config.Authority, settings?.Authority, count);
    }

    public async Task<long> RemoveAll()
    {
        var count = await externalAuthenticationRepository.CountAsync(OidcLinks());
        await externalAuthenticationRepository.DeleteManyAsync(x =>
            x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName);
        //their refresh tokens belong to the previous provider
        var sessions = await oidcSessionService.EndAllOpen();
        await Adopt();
        logger.LogWarning("All {Count} OpenID Connect links removed and {Sessions} provider sessions ended; links now " +
                          "belong to {Authority}", count, sessions, config.Authority);
        return count;
    }

    public async Task KeepAll()
    {
        await Adopt();
        logger.LogWarning("OpenID Connect links kept for {Authority} (the same provider at a new address)", config.Authority);
    }

    public async Task<bool> Remove(Customer customer, string linkId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (string.IsNullOrEmpty(linkId))
            return false;

        //only a link of this customer
        var link = await externalAuthenticationRepository.GetOneAsync(x => x.Id == linkId && x.CustomerId == customer.Id);
        if (link == null)
            return false;

        await externalAuthenticationRepository.DeleteAsync(link);
        if (link.ProviderSystemName == OidcCustomerResolver.ProviderSystemName)
            await oidcSessionService.EndByCustomer(customer.Id);
        logger.LogInformation("External link {Provider} of customer {CustomerId} removed", link.ProviderSystemName, customer.Id);
        return true;
    }

    private async Task Adopt()
    {
        var authority = OidcAuthority.Normalize(config.Authority);
        //nothing to record while the resource server has no provider
        if (string.IsNullOrEmpty(authority))
            return;

        await AdoptLock.WaitAsync();
        try
        {
            //another request may have recorded it while this one waited
            var settings = await settingService.LoadSetting<OidcLinkSettings>();
            if (OidcAuthority.SameProvider(settings?.Authority, authority) && !string.IsNullOrEmpty(settings?.Authority))
                return;
            await settingService.SaveSetting(new OidcLinkSettings { Authority = authority });
        }
        finally
        {
            AdoptLock.Release();
        }
    }

    private IQueryable<ExternalAuthentication> OidcLinks() =>
        externalAuthenticationRepository.Table.Where(x => x.ProviderSystemName == OidcCustomerResolver.ProviderSystemName);

    private Task<bool> HasLinks() => externalAuthenticationRepository.AnyAsync(OidcLinks());

    private void LogMismatch(string linkedAuthority)
    {
        var now = DateTime.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastMismatchLogTicks);
        if (now - last < TimeSpan.FromMinutes(5).Ticks ||
            Interlocked.CompareExchange(ref _lastMismatchLogTicks, now, last) != last)
            return;
        logger.LogError("The OpenID Connect links belong to {Linked}, not to OidcResourceServer:Authority {Configured}: " +
                        "OpenID Connect sign-in and access tokens are refused until the links are removed or kept (Admin > " +
                        "Configuration > External authentication)",
            string.IsNullOrEmpty(linkedAuthority) ? "a provider that was not recorded" : linkedAuthority, config.Authority);
    }
}
