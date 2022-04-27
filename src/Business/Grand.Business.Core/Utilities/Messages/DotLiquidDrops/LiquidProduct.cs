using DotLiquid;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidProduct : Drop
    {
        private readonly Product _product;
        private readonly Language _language;
        private readonly Store _store;
        private readonly DomainHost _host;
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

        public string Id {
            get { return _product.Id; }
        }

        public string Name {
            get { return _product.GetTranslation(x => x.Name, _language.Id); }
        }

        public string ShortDescription {
            get { return _product.GetTranslation(x => x.ShortDescription, _language.Id); }
        }

        public string SKU {
            get { return _product.Sku; }
        }

        public string ExternalId {
            get { return _product.ExternalId; }
        }

        public string Mpn {
            get { return _product.Mpn; }
        }

        public string Gtin {
            get { return _product.Gtin; }
        }

        public double AdditionalShippingCharge {
            get { return _product.AdditionalShippingCharge; }
        }

        public int StockQuantity {
            get { return _product.StockQuantity; }
        }



        public double Price {
            get { return _product.Price; }
        }

        public double CatalogPrice {
            get { return _product.CatalogPrice; }
        }

        public double OldPrice {
            get { return _product.OldPrice; }
        }

        public string Flag {
            get { return _product.Flag; }
        }

        public string ProductURLForCustomer {
            get { return string.Format("{0}/{1}", url, _product.GetSeName(_language.Id)); }
        }

        public double Weight {
            get { return _product.Weight; }
        }

        public double Length {
            get { return _product.Length; }
        }

        public double Width {
            get { return _product.Width; }
        }

        public double Height {
            get { return _product.Height; }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}