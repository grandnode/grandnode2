using DotLiquid;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidProduct : Drop
{
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly Product _product;
    private readonly Store _store;
    private readonly string url;

    public LiquidProduct(Product product, Language language, Store store, DomainHost host)
    {
        _product = product;
        _language = language;
        _store = store;
        _host = host;
        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Id => _product.Id;

    public string Name => _product.GetTranslation(x => x.Name, _language.Id);

    public string ShortDescription => _product.GetTranslation(x => x.ShortDescription, _language.Id);

    public string SKU => _product.Sku;

    public string ExternalId => _product.ExternalId;

    public string Mpn => _product.Mpn;

    public string Gtin => _product.Gtin;

    public double AdditionalShippingCharge => _product.AdditionalShippingCharge;

    public int StockQuantity => _product.StockQuantity;


    public double Price => _product.Price;

    public double CatalogPrice => _product.CatalogPrice;

    public double OldPrice => _product.OldPrice;

    public string Flag => _product.Flag;

    public string ProductURLForCustomer => $"{url}/{_product.GetSeName(_language.Id)}";

    public double Weight => _product.Weight;

    public double Length => _product.Length;

    public double Width => _product.Width;

    public double Height => _product.Height;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}