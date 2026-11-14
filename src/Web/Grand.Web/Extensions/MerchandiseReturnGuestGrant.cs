using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace Grand.Web.Extensions;

/// <summary>
///     The way a guest who ordered without an account returns items: a time-limited link sent to the order's e-mail
///     address leaves a grant for that one order in the browser. Both are protected payloads stored nowhere else
/// </summary>
public static class MerchandiseReturnGuestGrant
{
    public const string LinkPurpose = "Grand.Web.MerchandiseReturn.GuestLink";
    public const string GrantPurpose = "Grand.Web.MerchandiseReturn.GuestGrant";
    public const string CookieName = ".Grand.MerchandiseReturnGuest";

    public static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan GrantLifetime = TimeSpan.FromHours(2);

    /// <summary>
    ///     The token of the link sent to the order's e-mail address
    /// </summary>
    public static string LinkToken(IDataProtectionProvider provider, Order order)
    {
        return Protector(provider, LinkPurpose).Protect(Payload(order), DateTimeOffset.UtcNow + LinkLifetime);
    }

    /// <summary>
    ///     The order a link's token opens in the store; null when it expired, was changed or is of another store
    /// </summary>
    public static string LinkOrderId(IDataProtectionProvider provider, string token, string storeId)
    {
        return OrderId(provider, LinkPurpose, token, storeId);
    }

    /// <summary>
    ///     Leaves the grant for the order in the browser
    /// </summary>
    public static void Grant(HttpContext httpContext, Order order)
    {
        var expiration = DateTimeOffset.UtcNow + GrantLifetime;
        var provider = httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
        httpContext.Response.Cookies.Append(CookieName,
            Protector(provider, GrantPurpose).Protect(Payload(order), expiration),
            new CookieOptions {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", Expires = expiration,
                //the visitor asked for it with the link, as for the session cookie
                IsEssential = true
            });
    }

    /// <summary>
    ///     The order the browser holds a grant for in the store; null without one
    /// </summary>
    public static string GrantedOrderId(HttpContext httpContext, string storeId)
    {
        //nothing is resolved for the requests of everyone else
        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var cookie))
            return null;

        return OrderId(httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>(), GrantPurpose,
            cookie, storeId);
    }

    /// <summary>
    ///     The customer of an order the guest flow serves: an order of the store placed without an account. A
    ///     customer who has registered since logs in instead
    /// </summary>
    public static async Task<Customer> GuestOfOrder(Order order, string storeId, ICustomerService customerService,
        IGroupService groupService)
    {
        if (order == null || order.Deleted || order.StoreId != storeId)
            return null;

        var customer = await customerService.GetCustomerById(order.CustomerId);
        if (customer == null || await groupService.IsRegistered(customer))
            return null;

        return customer;
    }

    private static ITimeLimitedDataProtector Protector(IDataProtectionProvider provider, string purpose)
    {
        return provider.CreateProtector(purpose).ToTimeLimitedDataProtector();
    }

    private static string Payload(Order order)
    {
        return $"{order.Id}|{order.StoreId}";
    }

    private static string OrderId(IDataProtectionProvider provider, string purpose, string token, string storeId)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        string payload;
        try
        {
            payload = Protector(provider, purpose).Unprotect(token);
        }
        catch (CryptographicException)
        {
            //expired, changed, or protected for another purpose
            return null;
        }

        var parts = payload.Split('|');
        return parts.Length == 2 && parts[1] == storeId ? parts[0] : null;
    }
}
