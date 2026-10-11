using Grand.Business.Authentication.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcTokenRevocationServiceTests
{
    private IRepository<ExternalAuthentication> _links;
    private OidcTokenRevocationService _service;

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _service = new OidcTokenRevocationService(_links);
    }

    private Task Link(string customerId, string sub, string provider = OidcCustomerResolver.ProviderSystemName, string storeId = "") =>
        _links.InsertAsync(new ExternalAuthentication {
            CustomerId = customerId, ExternalIdentifier = sub, ProviderSystemName = provider, StoreId = storeId
        });

    [TestMethod]
    public async Task NeverRevoked_NotRevoked()
    {
        await Link("c1", "sub-1");
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddHours(-1)));
    }

    [TestMethod]
    public async Task Revoke_OlderTokenRevoked_NewerAccepted()
    {
        await Link("c1", "sub-1");
        await _service.RevokeTokens("sub-1");
        Assert.IsTrue(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddMinutes(-1)));
        Assert.IsFalse(await _service.IsRevoked("sub-1", DateTime.UtcNow.AddMinutes(1)));
    }

    //with per-store identity a subject has one link per store: all of them stop the old tokens
    [TestMethod]
    public async Task Revoke_EveryLinkOfTheSubject_OnlyOpenIdConnect()
    {
        await Link("c1", "sub-1", storeId: "s1");
        await Link("c2", "sub-1", storeId: "s2");
        await Link("c3", "sub-1", provider: "ExternalAuth.Google");
        await Link("c4", "sub-2");
        await _service.RevokeTokens("sub-1");

        var revoked = _links.Table.Where(x => x.TokensRevokedBeforeUtc != null).Select(x => x.CustomerId).ToList();
        CollectionAssert.AreEquivalent(new[] { "c1", "c2" }, revoked);
    }

    [TestMethod]
    public async Task UnknownSubject_NotRevoked_RevokeDoesNothing()
    {
        await _service.RevokeTokens("nobody");
        Assert.IsFalse(await _service.IsRevoked("nobody", DateTime.UtcNow.AddHours(-1)));
    }
}
