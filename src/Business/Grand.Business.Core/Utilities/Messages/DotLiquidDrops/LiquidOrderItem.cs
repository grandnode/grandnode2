using DotLiquid;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidOrderItem : Drop
    {
        private readonly OrderItem _orderItem;
        private readonly Product _product;
        private readonly Language _language;
        private readonly Store _store;
        private readonly DomainHost _host;
        private readonly Vendor _vendor;

        private string url;

        public LiquidOrderItem(OrderItem orderItem, Product product, Language language, Store store, DomainHost host, Vendor vendor)
        {
            _orderItem = orderItem;
            _store = store;
            _language = language;
            _product = product;
            _vendor = vendor;
            _host = host;

            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string UnitPrice { get; set; }

        public string TotalPrice { get; set; }

        public string ProductSku { get; set; }

        public bool IsDownloadAllowed { get; set; }

        public bool IsLicenseDownloadAllowed { get; set; }

        public string DownloadUrl {
            get {
                string downloadUrl = string.Format("{0}/download/getdownload/{1}", url, _orderItem.OrderItemGuid);
                return downloadUrl;
            }
        }

        public string LicenseUrl {
            get {
                string licenseUrl = string.Format("{0}/download/getlicense/{1}", url, _orderItem.OrderItemGuid);
                return licenseUrl;
            }
        }

        public Guid OrderItemGuid {
            get {
                return _orderItem.OrderItemGuid;
            }
        }

        public string ProductName {
            get {
                string name = "";

                if (_product != null)
                    name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _language.Id));

                return name;
            }
        }

        public string ProductSeName {
            get {
                string name = "";

                if (_product != null)
                    name = _product.GetTranslation(x => x.SeName, _language.Id);
                return name;
            }
        }
        public string ProductShortDescription {
            get {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlEncode(_product.GetTranslation(x => x.ShortDescription, _language.Id));

                return desc;
            }
        }

        public string ProductFullDescription {
            get {
                string desc = "";

                if (_product != null)
                    desc = WebUtility.HtmlDecode(_product.GetTranslation(x => x.FullDescription, _language.Id));

                return desc;
            }
        }

        public string ProductId {
            get {
                return _orderItem.ProductId;
            }
        }

        public string VendorId {
            get {
                return _orderItem.VendorId;
            }
        }

        public string VendorName {
            get {
                return _vendor?.Name;
            }
        }

        public string WarehouseId {
            get {
                return _orderItem.WarehouseId;
            }
        }

        public int Quantity {
            get {
                return _orderItem.Quantity;
            }
        }

        public int OpenQty {
            get {
                return _orderItem.OpenQty;
            }
        }

        public int CancelQty {
            get {
                return _orderItem.CancelQty;
            }
        }
        public int ShipQty {
            get {
                return _orderItem.ShipQty;
            }
        }

        public bool IsShipEnabled {
            get {
                return _orderItem.IsShipEnabled;
            }
        }

        public double UnitPriceWithoutDiscInclTax {
            get {
                return _orderItem.UnitPriceWithoutDiscInclTax;
            }
        }

        public double UnitPriceWithoutDiscExclTax {
            get {
                return _orderItem.UnitPriceWithoutDiscExclTax;
            }
        }

        public double UnitPriceInclTax {
            get {
                return _orderItem.UnitPriceInclTax;
            }
        }

        public double UnitPriceExclTax {
            get {
                return _orderItem.UnitPriceExclTax;
            }
        }

        public double PriceInclTax {
            get {
                return _orderItem.PriceInclTax;
            }
        }

        public double PriceExclTax {
            get {
                return _orderItem.PriceExclTax;
            }
        }

        public double DiscountAmountInclTax {
            get {
                return _orderItem.DiscountAmountInclTax;
            }
        }

        public double DiscountAmountExclTax {
            get {
                return _orderItem.DiscountAmountExclTax;
            }
        }

        public double OriginalProductCost {
            get {
                return _orderItem.OriginalProductCost;
            }
        }

        public string AttributeDescription {
            get {
                return _orderItem.AttributeDescription;
            }
        }

        public int DownloadCount {
            get {
                return _orderItem.DownloadCount;
            }
        }

        public bool IsDownloadActivated {
            get {
                return _orderItem.IsDownloadActivated;
            }
        }

        public string LicenseDownloadId {
            get {
                return _orderItem.LicenseDownloadId;
            }
        }

        public double? ItemWeight {
            get {
                return _orderItem.ItemWeight;
            }
        }

        public double ProductWeight {
            get { return _product.Weight; }
        }

        public double ProductLength {
            get { return _product.Length; }
        }

        public double ProductWidth {
            get { return _product.Width; }
        }

        public double ProductHeight {
            get { return _product.Height; }
        }

        public double ProductCatalogPrice {
            get { return _product.CatalogPrice; }
        }

        public double ProductOldPrice {
            get { return _product.OldPrice; }
        }

        public string ProductFlag {
            get { return _product.Flag; }
        }

        public DateTime? RentalStartDateUtc {
            get {
                return _orderItem.RentalStartDateUtc;
            }
        }

        public DateTime? RentalEndDateUtc {
            get {
                return _orderItem.RentalEndDateUtc;
            }
        }

        public DateTime CreatedOnUtc {
            get {
                return _orderItem.CreatedOnUtc;
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}