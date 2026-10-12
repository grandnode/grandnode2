using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcSessionServiceTests
{
    private IRepository<OidcSession> _sessions;
    private Mock<ICacheBase> _cache;
    private Mock<IPermissionService> _permissions;
    private Mock<IOidcTokenRevocationService> _revocation;
    private Mock<IOidcTokenRefresher> _refresher;
    private List<IOidcTokenRefresher> _refreshers;
    private IDataProtectionProvider _dataProtection;
    private OidcLoginConfig _config;
    private SecurityConfig _securityConfig;
    private bool _staff;

    private readonly Customer _customer = new() { Id = "c1" };

    [TestInitialize]
    public void Init()
    {
        _sessions = new MongoDBRepositoryTest<OidcSession>();
        _cache = new Mock<ICacheBase>();
        //read-through without caching: every call sees the repository
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<OidcSession>>>(), It.IsAny<int>()))
            .Returns((string _, Func<Task<OidcSession>> acquire, int _) => acquire());
        _permissions = new Mock<IPermissionService>();
        _permissions.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, It.IsAny<Customer>()))
            .ReturnsAsync(() => _staff);
        _revocation = new Mock<IOidcTokenRevocationService>();
        _refresher = new Mock<IOidcTokenRefresher>();
        _refreshers = [_refresher.Object];
        _dataProtection = new EphemeralDataProtectionProvider();
        _config = new OidcLoginConfig();
        _securityConfig = new SecurityConfig { CookieAuthExpires = 24 };
        _staff = false;
    }

    private OidcSessionService Service() =>
        new(_sessions, _cache.Object, _permissions.Object, _revocation.Object, _refreshers, _dataProtection, _config,
            _securityConfig, NullLogger<OidcSessionService>.Instance);

    private string Protect(string token) =>
        _dataProtection.CreateProtector(OidcSessionDefaults.RefreshTokenPurpose).Protect(token);

    private string Unprotect(string token) =>
        _dataProtection.CreateProtector(OidcSessionDefaults.RefreshTokenPurpose).Unprotect(token);

    //a session as it would be stored, with clocks moved back by the given amounts
    private async Task<OidcSession> Session(TimeSpan? age = null, TimeSpan? idle = null, TimeSpan? sinceValidated = null,
        string refreshToken = "rt-1", string sid = "sid-1", string subject = "sub-1", string customerId = "c1")
    {
        var now = DateTime.UtcNow;
        var session = new OidcSession {
            CustomerId = customerId, StoreId = "s1", Subject = subject, Sid = sid,
            CreatedUtc = now - (age ?? TimeSpan.Zero),
            LastActivityUtc = now - (idle ?? TimeSpan.Zero),
            LastValidatedUtc = now - (sinceValidated ?? TimeSpan.Zero),
            ProtectedRefreshToken = refreshToken == null ? null : Protect(refreshToken)
        };
        await _sessions.InsertAsync(session);
        return session;
    }

    private OidcSession Stored(string id) => _sessions.Table.First(x => x.Id == id);

    [TestMethod]
    public async Task Start_Customer_RefreshTokenProtected()
    {
        var id = await Service().Start(_customer, "sub-1", "sid-1", "rt-1", "s1");
        var session = Stored(id);
        Assert.AreEqual("rt-1", Unprotect(session.ProtectedRefreshToken));
        Assert.AreEqual("sid-1", session.Sid);
    }

    [TestMethod]
    public async Task Start_Staff_RefreshTokenProtected()
    {
        _staff = true;
        var id = await Service().Start(_customer, "sub-1", null, "rt-1", "s1");
        var session = Stored(id);
        Assert.AreNotEqual("rt-1", session.ProtectedRefreshToken);
        Assert.AreEqual("rt-1", Unprotect(session.ProtectedRefreshToken));
        Assert.AreEqual("", session.Sid);
    }

    //any account: a session without a refresh token could never be revalidated
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Start_WithoutRefreshToken_Null(bool staff)
    {
        _staff = staff;
        Assert.IsNull(await Service().Start(_customer, "sub-1", "sid-1", null, "s1"));
        Assert.AreEqual(0, _sessions.Table.Count());
    }

    [TestMethod]
    public async Task End_SetsReason_OnlyOnce()
    {
        var session = await Session();
        await Service().End(session.Id, OidcSessionEndReason.SignOut);
        var ended = Stored(session.Id).EndedUtc;
        await Service().End(session.Id, OidcSessionEndReason.Idle);
        Assert.AreEqual(OidcSessionEndReason.SignOut, Stored(session.Id).EndReason);
        Assert.AreEqual(ended, Stored(session.Id).EndedUtc);
        _cache.Verify(x => x.RemoveAsync(string.Format(CacheKey.OIDC_SESSION_BY_ID_KEY, session.Id), true), Times.AtLeastOnce);
    }

    //an ended session is never confirmed again: its refresh token is not kept for the retention period
    [TestMethod]
    public async Task End_ClearsRefreshToken()
    {
        var session = await Session();
        await Service().End(session.Id, OidcSessionEndReason.SignOut);
        Assert.IsNull(Stored(session.Id).ProtectedRefreshToken);
    }

    [TestMethod]
    public async Task EndBySid_MatchingSidAndSubject_Only()
    {
        var match = await Session(sid: "sid-1", subject: "sub-1");
        var otherSid = await Session(sid: "sid-2", subject: "sub-1");
        var otherSubject = await Session(sid: "sid-1", subject: "sub-2");

        var ended = await Service().EndBySid("sub-1", "sid-1");

        CollectionAssert.AreEqual(new[] { match.Id }, ended.Select(x => x.Id).ToArray());
        Assert.AreEqual(OidcSessionEndReason.BackChannel, Stored(match.Id).EndReason);
        Assert.IsNull(Stored(otherSid.Id).EndedUtc);
        Assert.IsNull(Stored(otherSubject.Id).EndedUtc);
    }

    //a logout token with sid only: every session of that provider session, whatever the subject
    [TestMethod]
    public async Task EndBySid_NoSubject_AllWithSid()
    {
        var a = await Session(sid: "sid-1", subject: "sub-1");
        var b = await Session(sid: "sid-1", subject: "sub-2");
        var ended = await Service().EndBySid(null, "sid-1");
        CollectionAssert.AreEquivalent(new[] { a.Id, b.Id }, ended.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task EndBySubject_AllOpenSessionsOfSubject()
    {
        var a = await Session(sid: "sid-1");
        var b = await Session(sid: "");
        var other = await Session(subject: "sub-2");
        var ended = await Service().EndBySubject("sub-1");
        CollectionAssert.AreEquivalent(new[] { a.Id, b.Id }, ended.Select(x => x.Id).ToArray());
        Assert.IsNull(Stored(other.Id).EndedUtc);
    }

    [TestMethod]
    public async Task DeleteExpired_KeepsRecentAndOpen()
    {
        _securityConfig.CookieAuthExpires = 24;
        var endedLongAgo = await Session();
        await _sessions.UpdateField(endedLongAgo.Id, x => x.EndedUtc, DateTime.UtcNow.AddDays(-8));
        var endedRecently = await Session();
        await _sessions.UpdateField(endedRecently.Id, x => x.EndedUtc, DateTime.UtcNow.AddDays(-1));
        //past the cookie lifetime (24 h here) + retention, whoever it belonged to
        var expired = await Session(age: TimeSpan.FromHours(24) + TimeSpan.FromDays(8));
        var open = await Session(age: TimeSpan.FromHours(30));

        await Service().DeleteExpired();

        CollectionAssert.AreEquivalent(new[] { endedRecently.Id, open.Id }, _sessions.Table.Select(x => x.Id).ToArray());
    }

    //every session keeps a refresh token now: the old staff rule (refresh token kept + past staff maximum) would delete live customers
    [TestMethod]
    public async Task DeleteExpired_KeepsActiveCustomerWithRefreshToken()
    {
        _securityConfig.CookieAuthExpires = 8760;
        var customer = await Session(age: TimeSpan.FromDays(20));
        await Service().DeleteExpired();
        Assert.AreEqual(1, _sessions.Table.Count(x => x.Id == customer.Id));
    }

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    [TestMethod]
    public async Task Validate_MissingOrEmptyId_False()
    {
        Assert.IsFalse(await Service().Validate("000000000000000000000000", _customer));
        Assert.IsFalse(await Service().Validate("", _customer));
    }

    [TestMethod]
    public async Task Validate_Ended_False()
    {
        var session = await Session();
        await Service().End(session.Id, OidcSessionEndReason.BackChannel);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
    }

    //a cookie whose session claim was copied into another customer's ticket
    [TestMethod]
    public async Task Validate_OtherCustomer_False()
    {
        var session = await Session(customerId: "c2");
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
    }

    //customers keep the long session: no idle or maximum lifetime, only the provider check
    [TestMethod]
    public async Task Validate_Customer_OldAndIdle_WithinInterval_True_NoRefresh()
    {
        var session = await Session(age: TimeSpan.FromDays(30), idle: TimeSpan.FromDays(20),
            sinceValidated: TimeSpan.FromMinutes(5));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        _refresher.Verify(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsNull(Stored(session.Id).EndedUtc);
    }

    [TestMethod]
    public async Task Validate_CustomerRefused_EndsAndRevokesTokens()
    {
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Refused());
        var session = await Session(age: TimeSpan.FromDays(3), idle: TimeSpan.FromDays(2), sinceValidated: Interval);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.RevalidationRefused, Stored(session.Id).EndReason);
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
    }

    [TestMethod]
    public async Task Validate_CustomerRefreshed_RotatedTokenStored()
    {
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Refreshed("rt-2"));
        var session = await Session(sinceValidated: Interval);
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.AreEqual("rt-2", Unprotect(Stored(session.Id).ProtectedRefreshToken));
    }

    [TestMethod]
    public async Task Validate_CustomerUnavailable_WithinGrace_Valid()
    {
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Unavailable());
        var session = await Session(sinceValidated: Interval + TimeSpan.FromMinutes(5));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
    }

    //a customer session started before every session kept a refresh token
    [TestMethod]
    public async Task Validate_CustomerWithoutRefreshToken_Ends()
    {
        var session = await Session(refreshToken: null);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.NoRefreshToken, Stored(session.Id).EndReason);
    }

    //customers' activity is not written: only the staff idle rule needs it
    [TestMethod]
    public async Task Validate_Customer_ActivityNotWritten()
    {
        var session = await Session(idle: TimeSpan.FromMinutes(5));
        var before = Stored(session.Id).LastActivityUtc;
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(before, Stored(session.Id).LastActivityUtc);
    }

    [TestMethod]
    public async Task Validate_StaffIdle_Ends()
    {
        _staff = true;
        var session = await Session(idle: TimeSpan.FromMinutes(31));
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.Idle, Stored(session.Id).EndReason);
    }

    [TestMethod]
    public async Task Validate_StaffPastMaximum_Ends()
    {
        _staff = true;
        var session = await Session(age: TimeSpan.FromHours(10) + TimeSpan.FromMinutes(1));
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.MaxLifetime, Stored(session.Id).EndReason);
    }

    //a customer promoted to staff during the session has no refresh token to check the account with
    [TestMethod]
    public async Task Validate_StaffWithoutRefreshToken_Ends()
    {
        _staff = true;
        var session = await Session(refreshToken: null);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.NoRefreshToken, Stored(session.Id).EndReason);
    }

    [TestMethod]
    public async Task Validate_StaffWithinInterval_NoRefresh()
    {
        _staff = true;
        var session = await Session(sinceValidated: TimeSpan.FromMinutes(9));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        _refresher.Verify(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Validate_StaffRefreshed_RotatedTokenStored()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Refreshed("rt-2"));
        var session = await Session(sinceValidated: Interval);
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        var stored = Stored(session.Id);
        Assert.AreEqual("rt-2", Unprotect(stored.ProtectedRefreshToken));
        Assert.IsTrue(stored.LastValidatedUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [TestMethod]
    public async Task Validate_StaffRefreshedWithoutRotation_OldTokenKept()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Refreshed(null));
        var session = await Session(sinceValidated: Interval);
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.AreEqual("rt-1", Unprotect(Stored(session.Id).ProtectedRefreshToken));
    }

    [TestMethod]
    public async Task Validate_StaffRefused_EndsAndRevokesTokens()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Refused());
        var session = await Session(sinceValidated: Interval);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.RevalidationRefused, Stored(session.Id).EndReason);
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
    }

    //another request (or instance) used the rotated token first and stored the new one: the refusal is about the old token
    [TestMethod]
    public async Task Refused_AfterAnotherRefreshStoredNewToken_StaysValid()
    {
        _staff = true;
        var session = await Session(sinceValidated: Interval);
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await _sessions.UpdateField(session.Id, x => x.ProtectedRefreshToken, Protect("rt-2"));
            return OidcRefreshResult.Refused();
        });
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.IsNull(Stored(session.Id).EndedUtc);
        _revocation.Verify(x => x.RevokeTokens(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Validate_StaffUnavailable_WithinTwoIntervals_Valid()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Unavailable());
        var session = await Session(sinceValidated: Interval + TimeSpan.FromMinutes(5));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.IsNull(Stored(session.Id).EndedUtc);
    }

    [TestMethod]
    public async Task Validate_StaffUnavailable_PastTwoIntervals_Ends()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(OidcRefreshResult.Unavailable());
        var session = await Session(sinceValidated: Interval * 2 + TimeSpan.FromMinutes(1));
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.RevalidationUnavailable, Stored(session.Id).EndReason);
        _revocation.Verify(x => x.RevokeTokens(It.IsAny<string>()), Times.Never);
    }

    //a refresher that fails is no answer from the provider: the session is kept within the grace time
    [TestMethod]
    public async Task Validate_RefresherThrows_TreatedAsUnavailable()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var session = await Session(sinceValidated: Interval + TimeSpan.FromMinutes(5));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.IsNull(Stored(session.Id).EndedUtc);
        _revocation.Verify(x => x.RevokeTokens(It.IsAny<string>()), Times.Never);
    }

    //a request holding a cached record from before another request's refresh must not use the already rotated token
    [TestMethod]
    public async Task Validate_CachedRecordStale_StoredRecentlyValidated_NoRefresh()
    {
        _staff = true;
        var session = await Session(sinceValidated: Interval + TimeSpan.FromMinutes(1));
        var stale = Stored(session.Id);
        await _sessions.UpdateField(session.Id, x => x.LastValidatedUtc, DateTime.UtcNow);
        await _sessions.UpdateField(session.Id, x => x.ProtectedRefreshToken, Protect("rt-2"));
        _cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<OidcSession>>>(), It.IsAny<int>()))
            .ReturnsAsync(stale);

        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        _refresher.Verify(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //the token was replaced by another instance while this refresh ran: its token is never overwritten
    [TestMethod]
    public async Task Refreshed_AfterAnotherRefreshStoredNewToken_NewerTokenKept()
    {
        _staff = true;
        var session = await Session(sinceValidated: Interval);
        _refresher.Setup(x => x.Refresh("rt-1", It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await _sessions.UpdateField(session.Id, x => x.ProtectedRefreshToken, Protect("rt-X"));
            return OidcRefreshResult.Refreshed("rt-2");
        });
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.AreEqual("rt-X", Unprotect(Stored(session.Id).ProtectedRefreshToken));
    }

    //no cancellation token is passed, so a cancellation is a provider timeout
    [TestMethod]
    public async Task Validate_RefresherCancelled_TreatedAsUnavailable()
    {
        _staff = true;
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());
        var session = await Session(sinceValidated: Interval + TimeSpan.FromMinutes(5));
        Assert.IsTrue(await Service().Validate(session.Id, _customer));
        Assert.IsNull(Stored(session.Id).EndedUtc);
    }

    //the login plugin was uninstalled: every session ends at the next check
    [TestMethod]
    public async Task NoRefresher_CountsAsRefused()
    {
        _staff = true;
        _refreshers.Clear();
        var session = await Session(sinceValidated: Interval);
        Assert.IsFalse(await Service().Validate(session.Id, _customer));
        Assert.AreEqual(OidcSessionEndReason.RevalidationRefused, Stored(session.Id).EndReason);
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
    }

    //a parallel request finds the refresh in progress and goes on while the last check is recent enough
    [TestMethod]
    public async Task RefreshInProgress_RecentCheck_Valid()
    {
        _staff = true;
        var release = new TaskCompletionSource<OidcRefreshResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource();
        _refresher.Setup(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                started.TrySetResult();
                return release.Task;
            });
        var session = await Session(sinceValidated: Interval);

        var first = Service().Validate(session.Id, _customer);
        try
        {
            //the first request holds the refresh before the second one starts
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.IsTrue(await Service().Validate(session.Id, _customer));
        }
        finally
        {
            //always released: the in-progress marker is process-wide and must not outlive the test
            release.TrySetResult(OidcRefreshResult.Refreshed("rt-2"));
        }

        Assert.IsTrue(await first);
        _refresher.Verify(x => x.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Validate_StaffActivity_WrittenAtMostOncePerMinute()
    {
        _staff = true;
        var fresh = await Session(idle: TimeSpan.FromSeconds(10));
        var before = Stored(fresh.Id).LastActivityUtc;
        Assert.IsTrue(await Service().Validate(fresh.Id, _customer));
        Assert.AreEqual(before, Stored(fresh.Id).LastActivityUtc);

        var stale = await Session(idle: TimeSpan.FromMinutes(2));
        Assert.IsTrue(await Service().Validate(stale.Id, _customer));
        Assert.IsTrue(Stored(stale.Id).LastActivityUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}
