using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Grand.Business.Authentication.Services;

/// <summary>
///     Browser sessions started by an identity provider sign-in. Every session can be ended on the server (sign-out,
///     back-channel logout); a back-office session also ends after the idle time or the maximum lifetime, and is
///     confirmed at the provider at an interval by exchanging its refresh token - the only way to notice that the
///     provider disabled or deleted the user, since providers send no logout for that
/// </summary>
public class OidcSessionService : IOidcSessionService
{
    //how often the activity time of a back-office session is written
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromMinutes(1);

    //minutes; ended sessions are removed from the cache, so only the activity time may be this stale
    private const int CacheTimeMinutes = 1;

    //one refresh at a time per session in this process: refresh tokens rotate
    private static readonly ConcurrentDictionary<string, byte> RefreshesInProgress = new();

    private readonly IRepository<OidcSession> _sessionRepository;
    private readonly ICacheBase _cache;
    private readonly IPermissionService _permissionService;
    private readonly IOidcTokenRevocationService _tokenRevocationService;
    private readonly IEnumerable<IOidcTokenRefresher> _tokenRefreshers;
    private readonly IDataProtector _protector;
    private readonly OidcLoginConfig _config;
    private readonly SecurityConfig _securityConfig;
    private readonly ILogger<OidcSessionService> _logger;

    public OidcSessionService(
        IRepository<OidcSession> sessionRepository,
        ICacheBase cache,
        IPermissionService permissionService,
        IOidcTokenRevocationService tokenRevocationService,
        IEnumerable<IOidcTokenRefresher> tokenRefreshers,
        IDataProtectionProvider dataProtectionProvider,
        OidcLoginConfig config,
        SecurityConfig securityConfig,
        ILogger<OidcSessionService> logger)
    {
        _sessionRepository = sessionRepository;
        _cache = cache;
        _permissionService = permissionService;
        _tokenRevocationService = tokenRevocationService;
        _tokenRefreshers = tokenRefreshers;
        _protector = dataProtectionProvider.CreateProtector(OidcSessionDefaults.RefreshTokenPurpose);
        _config = config;
        _securityConfig = securityConfig;
        _logger = logger;
    }

    public virtual async Task<string> Start(Customer customer, string subject, string sid, string refreshToken, string storeId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentException.ThrowIfNullOrEmpty(subject);

        var backOffice = await _permissionService.IsBackOfficeAccount(customer);
        if (backOffice && string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogError("Provider sign-in of back-office customer {CustomerId} refused: the provider issued no refresh " +
                             "token, so the session could not be revalidated. Add offline_access to OidcLogin:Scopes " +
                             "if the provider issues refresh tokens only for it", customer.Id);
            return null;
        }

        var now = DateTime.UtcNow;
        var session = new OidcSession {
            CustomerId = customer.Id,
            StoreId = storeId ?? "",
            Subject = subject,
            Sid = sid ?? "",
            CreatedUtc = now,
            LastActivityUtc = now,
            LastValidatedUtc = now,
            ProtectedRefreshToken = backOffice ? _protector.Protect(refreshToken) : null
        };
        await _sessionRepository.InsertAsync(session);
        return session.Id;
    }

    public virtual Task<bool> Validate(string sessionId, Customer customer) =>
        throw new NotImplementedException();

    public virtual async Task End(string sessionId, OidcSessionEndReason reason)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;
        //the first end wins: its reason is the one that explains the sign-out
        await _sessionRepository.UpdateOneAsync(x => x.Id == sessionId && x.EndedUtc == null,
            UpdateBuilder<OidcSession>.Create().Set(x => x.EndedUtc, DateTime.UtcNow).Set(x => x.EndReason, reason));
        await _cache.RemoveAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, sessionId));
        _logger.LogInformation("OpenID Connect session {SessionId} ended: {Reason}", sessionId, reason);
    }

    public virtual async Task<IList<OidcSession>> EndBySid(string subject, string sid)
    {
        ArgumentException.ThrowIfNullOrEmpty(sid);
        var query = _sessionRepository.Table.Where(x => x.Sid == sid && x.EndedUtc == null);
        if (!string.IsNullOrEmpty(subject))
            query = query.Where(x => x.Subject == subject);
        return await EndAll(await _sessionRepository.ToListAsync(query));
    }

    public virtual async Task<IList<OidcSession>> EndBySubject(string subject)
    {
        ArgumentException.ThrowIfNullOrEmpty(subject);
        return await EndAll(await _sessionRepository.ToListAsync(
            _sessionRepository.Table.Where(x => x.Subject == subject && x.EndedUtc == null)));
    }

    public virtual async Task DeleteExpired()
    {
        var now = DateTime.UtcNow;
        var ended = now - OidcSessionDefaults.Retention;
        await _sessionRepository.DeleteManyAsync(x => x.EndedUtc != null && x.EndedUtc < ended);

        //sessions nobody used again end lazily, at the next request: these will never get one
        var staffCreated = now - _config.StaffMaxSession - OidcSessionDefaults.Retention;
        await _sessionRepository.DeleteManyAsync(x => x.ProtectedRefreshToken != null && x.CreatedUtc < staffCreated);
        var customerCreated = now - TimeSpan.FromHours(_securityConfig.CookieAuthExpires) - OidcSessionDefaults.Retention;
        await _sessionRepository.DeleteManyAsync(x => x.CreatedUtc < customerCreated);
    }

    private async Task<IList<OidcSession>> EndAll(IList<OidcSession> sessions)
    {
        foreach (var session in sessions)
            await End(session.Id, OidcSessionEndReason.BackChannel);
        return sessions;
    }

    private Task<OidcSession> Get(string sessionId) =>
        _cache.GetAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, sessionId),
            () => _sessionRepository.GetByIdAsync(sessionId), CacheTimeMinutes);

    private string Unprotect(string protectedToken)
    {
        try
        {
            return _protector.Unprotect(protectedToken);
        }
        catch (CryptographicException)
        {
            //key ring lost or rotated away: the token cannot be used any more
            return null;
        }
    }
}
