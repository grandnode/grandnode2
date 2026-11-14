using DotLiquid;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

/// <summary>
///     The link that opens an order placed without an account. The token is stored nowhere, so it reaches the template here
/// </summary>
public class LiquidGuestOrderLink : Drop
{
    private readonly Order _order;
    private readonly string _token;
    private readonly string url;

    public LiquidGuestOrderLink(Order order, Store store, DomainHost host, string token)
    {
        _order = order;
        _token = token;
        url = host?.Url.Trim('/') ?? store.Url.Trim('/');
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string OrderCode => _order.Code;

    public string Url => $"{url}/guestorder/access?token={WebUtility.UrlEncode(_token)}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}
