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
///     back-channel logout); a back-office session also ends after the idle time or the maximum lifetime, and every
///     session is confirmed at the provider at an interval by exchanging its refresh token - the only way to notice
///     that the provider disabled or deleted the user, since providers send no logout for that
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

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogError("Provider sign-in of customer {CustomerId} refused: the provider issued no refresh token, so the " +
                             "session could not be revalidated. Request offline_access in OidcLogin:Scopes and allow the " +
                             "Refresh Token grant for the client at the provider", customer.Id);
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
            ProtectedRefreshToken = _protector.Protect(refreshToken)
        };
        await _sessionRepository.InsertAsync(session);
        return session.Id;
    }

    public virtual async Task<bool> Validate(string sessionId, Customer customer)
    {
        if (string.IsNullOrEmpty(sessionId) || customer == null)
            return false;

        var session = await Get(sessionId);
        if (session == null || session.EndedUtc.HasValue || session.CustomerId != customer.Id)
            return false;

        var now = DateTime.UtcNow;
        //back-office is evaluated now: an account promoted or demoted during the session follows its current role
        var backOffice = await _permissionService.IsBackOfficeAccount(customer);
        if (backOffice)
        {
            if (now - session.LastActivityUtc > _config.StaffIdleTimeout)
                return await EndInvalid(session, OidcSessionEndReason.Idle);
            if (now - session.CreatedUtc > _config.StaffMaxSession)
                return await EndInvalid(session, OidcSessionEndReason.MaxLifetime);
        }

        if (string.IsNullOrEmpty(session.ProtectedRefreshToken))
            return await EndInvalid(session, OidcSessionEndReason.NoRefreshToken);

        if (now - session.LastValidatedUtc >= _config.RevalidationInterval && !await Revalidate(session, now))
            return false;

        //only the staff idle rule reads the activity time
        if (backOffice && now - session.LastActivityUtc >= ActivityWriteInterval)
        {
            await _sessionRepository.UpdateField(session.Id, x => x.LastActivityUtc, now);
            await _cache.RemoveAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, session.Id));
        }

        return true;
    }

    private async Task<bool> EndInvalid(OidcSession session, OidcSessionEndReason reason)
    {
        await End(session.Id, reason);
        return false;
    }

    /// <summary>
    ///     Confirms the account at the provider. A refusal ends the session and refuses the subject's older access tokens;
    ///     no answer keeps the session until two intervals passed since the last confirmation
    /// </summary>
    private async Task<bool> Revalidate(OidcSession cached, DateTime now)
    {
        var grace = _config.RevalidationInterval * 2;
        if (!RefreshesInProgress.TryAdd(cached.Id, 0))
            return now - cached.LastValidatedUtc < grace;

        try
        {
            //the cached record may predate a refresh that just finished: its token is already rotated
            var session = await _sessionRepository.GetByIdAsync(cached.Id);
            if (session == null || session.EndedUtc.HasValue)
                return false;
            if (now - session.LastValidatedUtc < _config.RevalidationInterval)
                return true;

            var refresher = _tokenRefreshers.FirstOrDefault();
            var refreshToken = Unprotect(session.ProtectedRefreshToken);
            OidcRefreshResult result;
            var unreadable = refresher != null && refreshToken == null;
            if (refresher == null || refreshToken == null)
            {
                if (unreadable)
                    _logger.LogWarning("The stored refresh token of session {SessionId} cannot be read (Data Protection keys changed)",
                        session.Id);
                result = OidcRefreshResult.Refused();
            }
            else
            {
                try
                {
                    result = await refresher.Refresh(refreshToken);
                }
                catch (Exception ex)
                {
                    //no cancellation token is passed, so a cancellation is a provider timeout: no answer, like any failure
                    _logger.LogWarning("Refreshing session {SessionId} failed: {ExceptionType}", session.Id, ex.GetType().Name);
                    result = OidcRefreshResult.Unavailable();
                }
            }

            switch (result.Outcome)
            {
                case OidcRefreshOutcome.Refreshed:
                {
                    var update = UpdateBuilder<OidcSession>.Create().Set(x => x.LastValidatedUtc, now);
                    if (!string.IsNullOrEmpty(result.RefreshToken))
                        update.Set(x => x.ProtectedRefreshToken, _protector.Protect(result.RefreshToken));
                    //conditional on the token read: a newer token stored meanwhile by another instance is never overwritten
                    await _sessionRepository.UpdateOneAsync(
                        x => x.Id == session.Id && x.EndedUtc == null && x.ProtectedRefreshToken == session.ProtectedRefreshToken,
                        update);
                    await _cache.RemoveAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, session.Id));
                    return true;
                }
                case OidcRefreshOutcome.Refused:
                {
                    //the token may have been rotated by another instance in the meantime: then the refusal is about the old one
                    var current = await _sessionRepository.GetByIdAsync(session.Id);
                    if (current is { EndedUtc: null } && current.ProtectedRefreshToken != session.ProtectedRefreshToken)
                    {
                        await _cache.RemoveAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, session.Id));
                        return true;
                    }

                    if (!unreadable)
                        _logger.LogWarning("The identity provider refused the session {SessionId} of customer {CustomerId}",
                            session.Id, session.CustomerId);
                    await End(session.Id, OidcSessionEndReason.RevalidationRefused);
                    await _tokenRevocationService.RevokeTokens(session.Subject);
                    return false;
                }
                default:
                    _logger.LogWarning("The identity provider could not confirm session {SessionId}; last confirmed {LastValidatedUtc}",
                        session.Id, session.LastValidatedUtc);
                    if (now - session.LastValidatedUtc < grace)
                        return true;
                    await End(session.Id, OidcSessionEndReason.RevalidationUnavailable);
                    return false;
            }
        }
        finally
        {
            RefreshesInProgress.TryRemove(cached.Id, out _);
        }
    }

    public virtual async Task End(string sessionId, OidcSessionEndReason reason)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;
        //the first end wins: its reason is the one that explains the sign-out; an ended session is never confirmed
        //again, so its refresh token is not kept for the retention period
        await _sessionRepository.UpdateOneAsync(x => x.Id == sessionId && x.EndedUtc == null,
            UpdateBuilder<OidcSession>.Create().Set(x => x.EndedUtc, DateTime.UtcNow).Set(x => x.EndReason, reason)
                .Set(x => x.ProtectedRefreshToken, null));
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
        var created = now - TimeSpan.FromHours(_securityConfig.CookieAuthExpires) - OidcSessionDefaults.Retention;
        await _sessionRepository.DeleteManyAsync(x => x.CreatedUtc < created);
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
