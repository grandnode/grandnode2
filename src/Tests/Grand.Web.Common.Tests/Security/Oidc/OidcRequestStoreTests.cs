using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Oidc;

/// <summary>
///     The store whose URL is the audience base follows the request host only, never the store cookie
/// </summary>
[TestClass]
public class OidcRequestStoreTests
{
    private static readonly Store StoreA = Store("A", "https://a.test/", "a.test");
    private static readonly Store StoreB = Store("B", "https://b.test/", "b.test");

    private static Store Store(string id, string url, string host) => new() {
        Id = id, Url = url, Domains = [new DomainHost { HostName = host, Url = url }]
    };

    private static HttpContext Context(string host, params Store[] stores)
    {
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString(host);
        //store B's cookie and an ambient store context naming B: neither may decide the audience
        http.Request.Headers.Cookie = $"{CommonHelper.StoreCookieName}=B";

        var services = new ServiceCollection();
        services.AddSingleton(OidcTestHost.StoreService(stores));
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(http);
        services.AddSingleton(accessor.Object);
        services.AddSingleton<IStoreContextSetter, StoreContextSetter>();
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(StoreB);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);
        services.AddSingleton(contextAccessor.Object);
        http.RequestServices = services.BuildServiceProvider();
        return http;
    }

    [TestMethod]
    public async Task HostOfStoreA_WithStoreBCookie_StoreA() =>
        Assert.AreEqual("https://a.test", await OidcCanonicalUri.StoreBaseUrl(Context("a.test", StoreA, StoreB)));

    [TestMethod]
    public async Task HostWithPort_StoreListsHostWithPort_ThatStore() =>
        //the installer seeds HostName with the port of the install request
        Assert.AreEqual("https://c.test:8443", await OidcCanonicalUri.StoreBaseUrl(Context("c.test:8443", StoreA,
            Store("C", "https://c.test:8443/", "c.test:8443"))));

    [TestMethod]
    public async Task HostWithPort_StoreListsHostWithoutPort_ThatStore() =>
        Assert.AreEqual("https://a.test", await OidcCanonicalUri.StoreBaseUrl(Context("a.test:8443", StoreA, StoreB)));

    [TestMethod]
    public async Task UnknownHost_SeveralStores_Null() =>
        Assert.IsNull(await OidcCanonicalUri.StoreBaseUrl(Context("evil.test", StoreA, StoreB)));

    [TestMethod]
    public async Task UnknownHost_SingleStore_ThatStore() =>
        Assert.AreEqual("https://a.test", await OidcCanonicalUri.StoreBaseUrl(Context("localhost", StoreA)));
}
