using DotLiquid;
using Grand.Business.Common.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using System.Collections.Generic;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidProduct : Drop
    {
        private Product _product;
        private Language _language;
        private Store _store;

        public LiquidProduct(Product product, Language language, Store store)
        {
            _product = product;
            _language = language;
            _store = store;
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
            get { return string.Format("{0}/{1}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _product.GetSeName(_language.Id)); }
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