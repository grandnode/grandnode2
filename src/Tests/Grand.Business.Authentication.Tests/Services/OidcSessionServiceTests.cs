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
    public async Task Start_Customer_NoRefreshTokenKept()
    {
        var id = await Service().Start(_customer, "sub-1", "sid-1", "rt-1", "s1");
        var session = Stored(id);
        Assert.AreEqual("c1", session.CustomerId);
        Assert.AreEqual("sub-1", session.Subject);
        Assert.AreEqual("sid-1", session.Sid);
        Assert.AreEqual("s1", session.StoreId);
        Assert.IsNull(session.ProtectedRefreshToken);
        Assert.IsNull(session.EndedUtc);
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

    //a back-office sign-in without a refresh token could never be revalidated: no session, the sign-in fails
    [TestMethod]
    public async Task Start_StaffWithoutRefreshToken_Null()
    {
        _staff = true;
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
        var endedLongAgo = await Session();
        await _sessions.UpdateField(endedLongAgo.Id, x => x.EndedUtc, DateTime.UtcNow.AddDays(-8));
        var endedRecently = await Session();
        await _sessions.UpdateField(endedRecently.Id, x => x.EndedUtc, DateTime.UtcNow.AddDays(-1));
        //staff (refresh token kept) past the maximum lifetime + retention
        var oldStaff = await Session(age: TimeSpan.FromHours(10) + TimeSpan.FromDays(8));
        //customer (no refresh token) past the cookie lifetime (24 h here) + retention
        var oldCustomer = await Session(age: TimeSpan.FromHours(24) + TimeSpan.FromDays(8), refreshToken: null);
        var openCustomer = await Session(age: TimeSpan.FromHours(30), refreshToken: null);

        await Service().DeleteExpired();

        CollectionAssert.AreEquivalent(new[] { endedRecently.Id, openCustomer.Id },
            _sessions.Table.Select(x => x.Id).ToArray());
    }
}
