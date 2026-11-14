using DotLiquid;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

/// <summary>
///     The link a guest returns items of an order with. The token is stored nowhere, so it reaches the template here
/// </summary>
public class LiquidMerchandiseReturnGuestLink : Drop
{
    private readonly Order _order;
    private readonly string _token;
    private readonly string url;

    public LiquidMerchandiseReturnGuestLink(Order order, Store store, DomainHost host, string token)
    {
        _order = order;
        _token = token;
        url = host?.Url.Trim('/') ?? store.Url.Trim('/');
        AdditionalTokens = new Dictionary<string, string>();
    }

    public int OrderNumber => _order.OrderNumber;

    public string Url => $"{url}/guestmerchandisereturn/access?token={WebUtility.UrlEncode(_token)}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}
