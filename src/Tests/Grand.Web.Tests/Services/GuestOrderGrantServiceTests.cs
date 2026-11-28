using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Services;

/// <summary>
///     The link a guest gets by e-mail and the grant it leaves in the browser open one order of one store, for a
///     limited time, and only while the order's customer has no account
/// </summary>
[TestClass]
public class GuestOrderGrantServiceTests
{
    private readonly Order _order = new() { Id = "o1", StoreId = "s1", CustomerId = "g1" };
    private readonly Customer _guest = new() { Id = "g1" };
    private Mock<ICustomerService> _customers;
    private Mock<IGroupService> _groups;
    private IDataProtectionProvider _provider;

    private readonly SecurityConfig _securityConfig = new() {
        CookiePrefix = ".Grand.", CookieSecurePolicyAlways = true, CookieSameSite = SameSiteMode.Lax
    };

    public GuestOrderGrantServiceTests()
    {
        _provider = new EphemeralDataProtectionProvider();
        _customers = new Mock<ICustomerService>();
        _customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(_guest);
        _groups = new Mock<IGroupService>();
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(false);
    }

    private static DefaultHttpContext Context(string? cookie = null)
    {
        var context = new DefaultHttpContext();
        if (cookie != null)
            context.Request.Headers.Cookie = $".Grand.GuestOrder={cookie}";
        return context;
    }

    private GuestOrderGrantService Create(DefaultHttpContext? context = null)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context ?? Context());
        return new GuestOrderGrantService(accessor.Object, _provider, new CookieOptionsFactory(_securityConfig),
            new OrderSettings(), _customers.Object, _groups.Object);
    }

    private static string CookieValue(HttpContext context)
    {
        var header = context.Response.Headers.SetCookie.ToString();
        var start = header.IndexOf('=') + 1;
        return header[start..header.IndexOf(';')];
    }

    [TestMethod]
    public void LinkToken_RoundTrips_OrderId()
    {
        var token = Create().LinkToken(_order);

        Assert.AreEqual("o1", Create().LinkOrderId(token, "s1"));
    }

    [TestMethod]
    public void LinkOrderId_OtherStore_Null()
    {
        var token = Create().LinkToken(_order);

        Assert.IsNull(Create().LinkOrderId(token, "s2"));
    }

    [TestMethod]
    public void LinkOrderId_Tampered_Null()
    {
        var token = Create().LinkToken(_order);
        var middle = token.Length / 2;
        var tampered = token[..middle] + (token[middle] == 'A' ? 'B' : 'A') + token[(middle + 1)..];

        Assert.IsNull(Create().LinkOrderId(tampered, "s1"));
    }

    [TestMethod]
    public void LinkOrderId_Expired_Null()
    {
        var token = _provider.CreateProtector(GuestOrderGrantService.LinkPurpose).ToTimeLimitedDataProtector()
            .Protect("o1|s1", DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.IsNull(Create().LinkOrderId(token, "s1"));
    }

    [TestMethod]
    public void LinkToken_NotAcceptedAsGrant()
    {
        var token = Create().LinkToken(_order);

        Assert.IsNull(Create(Context(token)).GrantedOrderId("s1"));
    }

    [TestMethod]
    public void Grant_SetsProtectedCookie()
    {
        var context = Context();

        Create(context).Grant(_order);

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        StringAssert.StartsWith(header, ".grand.guestorder=");
        StringAssert.Contains(header, "httponly");
        StringAssert.Contains(header, "secure");
        StringAssert.Contains(header, "samesite=lax");
        StringAssert.Contains(header, "path=/");
        Assert.AreEqual("o1", Create(Context(CookieValue(context))).GrantedOrderId("s1"));
    }

    [TestMethod]
    public void Grant_BrowserSessionCookie()
    {
        //a shared computer does not keep it once the browser is closed; the payload still expires on its own
        var context = Context();

        Create(context).Grant(_order);

        Assert.IsFalse(context.Response.Headers.SetCookie.ToString().Contains("expires",
            StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Grant_FollowsStoreCookieSettings()
    {
        //a store served over plain http does not ask for the Secure flag, which the browser would refuse there
        _securityConfig.CookiePrefix = ".Shop.";
        _securityConfig.CookieSecurePolicyAlways = false;
        _securityConfig.CookieSameSite = SameSiteMode.Strict;
        var context = Context();

        Create(context).Grant(_order);

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        StringAssert.StartsWith(header, ".shop.guestorder=");
        Assert.IsFalse(header.Contains("secure"));
        StringAssert.Contains(header, "samesite=strict");
    }

    [TestMethod]
    public void GrantedOrderId_OtherStore_Null()
    {
        var context = Context();
        Create(context).Grant(_order);

        Assert.IsNull(Create(Context(CookieValue(context))).GrantedOrderId("s2"));
    }

    [TestMethod]
    public void GrantedOrderId_NoCookie_Null()
    {
        Assert.IsNull(Create(Context()).GrantedOrderId("s1"));
    }

    [TestMethod]
    public async Task GuestOfOrder_Guest_Customer()
    {
        Assert.AreSame(_guest,
            await Create().GuestOfOrder(_order, "s1"));
    }

    [TestMethod]
    public async Task GuestOfOrder_CustomerRegisteredMeanwhile_Null()
    {
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(true);

        Assert.IsNull(await Create().GuestOfOrder(_order, "s1"));
    }

    [TestMethod]
    public async Task GuestOfOrder_OtherStore_Null()
    {
        Assert.IsNull(await Create().GuestOfOrder(_order, "s2"));
    }

    [TestMethod]
    public async Task GuestOfOrder_Deleted_Null()
    {
        _order.Deleted = true;

        Assert.IsNull(await Create().GuestOfOrder(_order, "s1"));
    }

    [TestMethod]
    public async Task GuestOfOrder_NoCustomer_Null()
    {
        _order.CustomerId = "nobody";

        Assert.IsNull(await Create().GuestOfOrder(_order, "s1"));
    }
}
