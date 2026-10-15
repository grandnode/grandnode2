using Grand.Business.Authentication.Services;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcTokenRevocationServiceTests
{
    private IRepository<OidcTokenRevocation> _revocations;
    private OidcTokenRevocationService _service;

    [TestInitialize]
    public void Init()
    {
        _revocations = new MongoDBRepositoryTest<OidcTokenRevocation>();
        _service = new OidcTokenRevocationService(_revocations);
    }

    [TestMethod]
    public async Task NeverRevoked_NotRevoked()
    {
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddHours(-1)));
    }

    [TestMethod]
    public async Task Revoke_OlderTokenRevoked_NewerAccepted()
    {
        await _service.RevokeTokens("sub-1");
        Assert.IsTrue(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddMinutes(-1)));
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddMinutes(1)));
        Assert.IsFalse(await _service.IsRevoked("sub-2", DateTime.UtcNow.AddMinutes(-1)));
    }

    //a subject without a link yet (API tokens only) is revoked as well: a link made later does not bring the tokens back
    [TestMethod]
    public async Task Revoke_KeptApartFromLinks_OneRecordPerSubject()
    {
        await _service.RevokeTokens("sub-1");
        await _service.RevokeTokens("sub-1");

        Assert.AreEqual(1, _revocations.Table.Count(x => x.Subject == "sub-1"));
        Assert.AreEqual(OidcTokenRevocationService.RevocationId("sub-1"), _revocations.Table.Single().Id);
    }

    [TestMethod]
    public async Task RevokeAgain_MovesTheTimeForward()
    {
        var id = OidcTokenRevocationService.RevocationId("sub-1");
        await _revocations.InsertAsync(new OidcTokenRevocation {
            Id = id, Subject = "sub-1", RevokedBeforeUtc = DateTime.UtcNow.AddHours(-2)
        });
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddHours(-1)));

        await _service.RevokeTokens("sub-1");

        Assert.IsTrue(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddHours(-1)));
    }

    //subjects are case-sensitive
    [TestMethod]
    public async Task Revoke_SubjectCaseMatters()
    {
        await _service.RevokeTokens("Sub-1");
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddMinutes(-1)));
    }

    [TestMethod]
    public async Task DeleteExpired_KeepsRecentRevocations()
    {
        await _revocations.InsertAsync(new OidcTokenRevocation {
            Id = OidcTokenRevocationService.RevocationId("old"), Subject = "old",
            RevokedBeforeUtc = DateTime.UtcNow - OidcSessionDefaults.RevocationRetention - TimeSpan.FromHours(1)
        });
        await _service.RevokeTokens("recent");

        await _service.DeleteExpired();

        CollectionAssert.AreEqual(new[] { "recent" }, _revocations.Table.Select(x => x.Subject).ToList());
    }
}
