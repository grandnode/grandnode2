using DotLiquid;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidOutOfStockSubscription : Drop
{
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly OutOfStockSubscription _outOfStockSubscription;
    private readonly Product _product;
    private readonly Store _store;
    private readonly string url;

    public LiquidOutOfStockSubscription(Product product, OutOfStockSubscription outOfStockSubscription, Store store,
        DomainHost host, Language language)
    {
        _outOfStockSubscription = outOfStockSubscription;
        _product = product;
        _store = store;
        _language = language;
        _host = host;

        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string ProductName => _product.Name;

    public string AttributeInfo => _outOfStockSubscription.AttributeInfo;

    public string ProductUrl => $"{url}/{_product.SeName}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}