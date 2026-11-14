using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Extensions;

/// <summary>
///     The link a guest gets by e-mail and the grant it leaves in the browser open one order of one store, for a
///     limited time, and only while the order's customer has no account
/// </summary>
[TestClass]
public class MerchandiseReturnGuestGrantTests
{
    private readonly Order _order = new() { Id = "o1", StoreId = "s1", CustomerId = "g1" };
    private readonly Customer _guest = new() { Id = "g1" };
    private Mock<ICustomerService> _customers;
    private Mock<IGroupService> _groups;
    private IDataProtectionProvider _provider;

    private readonly SecurityConfig _securityConfig = new() {
        CookiePrefix = ".Grand.", CookieSecurePolicyAlways = true, CookieSameSite = SameSiteMode.Lax
    };

    [TestInitialize]
    public void Init()
    {
        _provider = new EphemeralDataProtectionProvider();
        _customers = new Mock<ICustomerService>();
        _customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(_guest);
        _groups = new Mock<IGroupService>();
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(false);
    }

    private DefaultHttpContext Context(string cookie = null)
    {
        var context = new DefaultHttpContext {
            RequestServices = new ServiceCollection().AddSingleton(_provider)
                .AddSingleton<ICookieOptionsFactory>(new CookieOptionsFactory(_securityConfig))
                .BuildServiceProvider()
        };
        if (cookie != null)
            context.Request.Headers.Cookie = $".Grand.MerchandiseReturnGuest={cookie}";
        return context;
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
        var token = MerchandiseReturnGuestGrant.LinkToken(_provider, _order);

        Assert.AreEqual("o1", MerchandiseReturnGuestGrant.LinkOrderId(_provider, token, "s1"));
    }

    [TestMethod]
    public void LinkOrderId_OtherStore_Null()
    {
        var token = MerchandiseReturnGuestGrant.LinkToken(_provider, _order);

        Assert.IsNull(MerchandiseReturnGuestGrant.LinkOrderId(_provider, token, "s2"));
    }

    [TestMethod]
    public void LinkOrderId_Tampered_Null()
    {
        var token = MerchandiseReturnGuestGrant.LinkToken(_provider, _order);
        var middle = token.Length / 2;
        var tampered = token[..middle] + (token[middle] == 'A' ? 'B' : 'A') + token[(middle + 1)..];

        Assert.IsNull(MerchandiseReturnGuestGrant.LinkOrderId(_provider, tampered, "s1"));
    }

    [TestMethod]
    public void LinkOrderId_Expired_Null()
    {
        var token = _provider.CreateProtector(MerchandiseReturnGuestGrant.LinkPurpose).ToTimeLimitedDataProtector()
            .Protect("o1|s1", DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.IsNull(MerchandiseReturnGuestGrant.LinkOrderId(_provider, token, "s1"));
    }

    [TestMethod]
    public void LinkToken_NotAcceptedAsGrant()
    {
        var token = MerchandiseReturnGuestGrant.LinkToken(_provider, _order);

        Assert.IsNull(MerchandiseReturnGuestGrant.GrantedOrderId(Context(token), "s1"));
    }

    [TestMethod]
    public void Grant_SetsProtectedCookie()
    {
        var context = Context();

        MerchandiseReturnGuestGrant.Grant(context, _order);

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        StringAssert.StartsWith(header, ".grand.merchandisereturnguest=");
        StringAssert.Contains(header, "httponly");
        StringAssert.Contains(header, "secure");
        StringAssert.Contains(header, "samesite=lax");
        StringAssert.Contains(header, "path=/");
        Assert.AreEqual("o1", MerchandiseReturnGuestGrant.GrantedOrderId(Context(CookieValue(context)), "s1"));
    }

    [TestMethod]
    public void Grant_BrowserSessionCookie()
    {
        //a shared computer does not keep it once the browser is closed; the payload still expires on its own
        var context = Context();

        MerchandiseReturnGuestGrant.Grant(context, _order);

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

        MerchandiseReturnGuestGrant.Grant(context, _order);

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        StringAssert.StartsWith(header, ".shop.merchandisereturnguest=");
        Assert.IsFalse(header.Contains("secure"));
        StringAssert.Contains(header, "samesite=strict");
    }

    [TestMethod]
    public void GrantedOrderId_OtherStore_Null()
    {
        var context = Context();
        MerchandiseReturnGuestGrant.Grant(context, _order);

        Assert.IsNull(MerchandiseReturnGuestGrant.GrantedOrderId(Context(CookieValue(context)), "s2"));
    }

    [TestMethod]
    public void GrantedOrderId_NoCookie_Null()
    {
        Assert.IsNull(MerchandiseReturnGuestGrant.GrantedOrderId(Context(), "s1"));
    }

    [TestMethod]
    public async Task GuestOfOrder_Guest_Customer()
    {
        Assert.AreSame(_guest,
            await MerchandiseReturnGuestGrant.GuestOfOrder(_order, "s1", _customers.Object, _groups.Object));
    }

    [TestMethod]
    public async Task GuestOfOrder_CustomerRegisteredMeanwhile_Null()
    {
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(true);

        Assert.IsNull(await MerchandiseReturnGuestGrant.GuestOfOrder(_order, "s1", _customers.Object, _groups.Object));
    }

    [TestMethod]
    public async Task GuestOfOrder_OtherStore_Null()
    {
        Assert.IsNull(await MerchandiseReturnGuestGrant.GuestOfOrder(_order, "s2", _customers.Object, _groups.Object));
    }

    [TestMethod]
    public async Task GuestOfOrder_Deleted_Null()
    {
        _order.Deleted = true;

        Assert.IsNull(await MerchandiseReturnGuestGrant.GuestOfOrder(_order, "s1", _customers.Object, _groups.Object));
    }

    [TestMethod]
    public async Task GuestOfOrder_NoCustomer_Null()
    {
        _order.CustomerId = "nobody";

        Assert.IsNull(await MerchandiseReturnGuestGrant.GuestOfOrder(_order, "s1", _customers.Object, _groups.Object));
    }
}
