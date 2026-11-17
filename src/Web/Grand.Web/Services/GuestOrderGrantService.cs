using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace Grand.Web.Services;

public class GuestOrderGrantService(
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    ICookieOptionsFactory cookieOptionsFactory,
    OrderSettings orderSettings,
    ICustomerService customerService,
    IGroupService groupService) : IGuestOrderGrantService
{
    public const string LinkPurpose = "Grand.Web.GuestOrder.Link";
    public const string GrantPurpose = "Grand.Web.GuestOrder.Grant";

    private static readonly TimeSpan LinkLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan GrantLifetime = TimeSpan.FromHours(2);

    public string CookieName => $"{cookieOptionsFactory.CookiePrefix}GuestOrder";

    public bool Allowed => orderSettings.AllowGuestOrderAccess;

    public bool ReturnsAllowed => Allowed && orderSettings.MerchandiseReturnsEnabled;

    public string LinkToken(Order order)
    {
        return Protector(dataProtectionProvider, LinkPurpose).Protect(Payload(order),
            DateTimeOffset.UtcNow + LinkLifetime);
    }

    public string LinkOrderId(string token, string storeId)
    {
        return OrderId(dataProtectionProvider, LinkPurpose, token, storeId);
    }

    public async Task<Customer> GuestOfOrder(Order order, string storeId)
    {
        if (order == null || order.Deleted || order.StoreId != storeId)
            return null;

        var customer = await customerService.GetCustomerById(order.CustomerId);
        if (customer == null || await groupService.IsRegistered(customer))
            return null;

        return customer;
    }

    public void Grant(Order order)
    {
        var options = cookieOptionsFactory.Create();
        //a cookie of the browser session: a shared computer does not keep it once the guest closes the browser
        options.Expires = null;
        //the visitor asked for it with the link, as for the session cookie
        options.IsEssential = true;
        httpContextAccessor.HttpContext!.Response.Cookies.Append(CookieName,
            Protector(dataProtectionProvider, GrantPurpose).Protect(Payload(order),
                DateTimeOffset.UtcNow + GrantLifetime),
            options);
    }

    public string GrantedOrderId(string storeId)
    {
        if (!httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(CookieName, out var cookie))
            return null;

        return OrderId(dataProtectionProvider, GrantPurpose, cookie, storeId);
    }

    public async Task<Customer> GrantedGuest(Order order, Customer visitor, string storeId)
    {
        if (order == null || GrantedOrderId(storeId) != order.Id ||
            !Allowed ||
            (visitor != null && await groupService.IsRegistered(visitor)))
            return null;

        return await GuestOfOrder(order, storeId);
    }

    public async Task<(Customer Customer, bool GuestLink)> OrderCustomer(Order order, Customer visitor, string storeId)
    {
        if (visitor != null && await order.Access(visitor, groupService))
            return (visitor, false);

        var guest = await GrantedGuest(order, visitor, storeId);
        return (guest, guest != null);
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
