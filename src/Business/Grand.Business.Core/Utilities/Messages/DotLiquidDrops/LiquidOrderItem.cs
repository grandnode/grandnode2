using DotLiquid;
using Grand.Business.Core.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidOrderItem : Drop
{
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly OrderItem _orderItem;
    private readonly Product _product;
    private readonly Store _store;
    private readonly Vendor _vendor;

    private readonly string url;

    public LiquidOrderItem(OrderItem orderItem, Product product, Language language, Store store, DomainHost host,
        Vendor vendor)
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
    public bool UnitPriceWithTax { get; set; }

    public string TotalPrice { get; set; }
    public bool TotalPriceWithTax { get; set; }

    public string ProductSku { get; set; }

    public bool IsDownloadAllowed { get; set; }

    public bool IsLicenseDownloadAllowed { get; set; }

    public string DownloadUrl {
        get {
            var downloadUrl = $"{url}/download/getdownload/{_orderItem.OrderItemGuid}";
            return downloadUrl;
        }
    }

    public string LicenseUrl {
        get {
            var licenseUrl = $"{url}/download/getlicense/{_orderItem.OrderItemGuid}";
            return licenseUrl;
        }
    }

    public Guid OrderItemGuid => _orderItem.OrderItemGuid;

    public string ProductName {
        get {
            var name = "";

            if (_product != null)
                name = WebUtility.HtmlEncode(_product.GetTranslation(x => x.Name, _language.Id));

            return name;
        }
    }

    public string ProductSeName {
        get {
            var name = "";

            if (_product != null)
                name = _product.GetTranslation(x => x.SeName, _language.Id);
            return name;
        }
    }

    public string ProductShortDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlEncode(_product.GetTranslation(x => x.ShortDescription, _language.Id));

            return desc;
        }
    }

    public string ProductFullDescription {
        get {
            var desc = "";

            if (_product != null)
                desc = WebUtility.HtmlDecode(_product.GetTranslation(x => x.FullDescription, _language.Id));

            return desc;
        }
    }

    public string ProductId => _orderItem.ProductId;

    public string VendorId => _orderItem.VendorId;

    public string VendorName => _vendor?.Name;

    public string WarehouseId => _orderItem.WarehouseId;

    public int Quantity => _orderItem.Quantity;

    public int OpenQty => _orderItem.OpenQty;

    public int CancelQty => _orderItem.CancelQty;

    public int ShipQty => _orderItem.ShipQty;

    public bool IsShipEnabled => _orderItem.IsShipEnabled;

    public double UnitPriceWithoutDiscInclTax => _orderItem.UnitPriceWithoutDiscInclTax;

    public double UnitPriceWithoutDiscExclTax => _orderItem.UnitPriceWithoutDiscExclTax;

    public double UnitPriceInclTax => _orderItem.UnitPriceInclTax;

    public double UnitPriceExclTax => _orderItem.UnitPriceExclTax;

    public double PriceInclTax => _orderItem.PriceInclTax;

    public double PriceExclTax => _orderItem.PriceExclTax;

    public double DiscountAmountInclTax => _orderItem.DiscountAmountInclTax;

    public double DiscountAmountExclTax => _orderItem.DiscountAmountExclTax;

    public double OriginalProductCost => _orderItem.OriginalProductCost;

    public string AttributeDescription => _orderItem.AttributeDescription;

    public int DownloadCount => _orderItem.DownloadCount;

    public bool IsDownloadActivated => _orderItem.IsDownloadActivated;

    public string LicenseDownloadId => _orderItem.LicenseDownloadId;

    public double? ItemWeight => _orderItem.ItemWeight;

    public double ProductWeight => _product.Weight;

    public double ProductLength => _product.Length;

    public double ProductWidth => _product.Width;

    public double ProductHeight => _product.Height;

    public double ProductCatalogPrice => _product.CatalogPrice;

    public double ProductOldPrice => _product.OldPrice;

    public string ProductFlag => _product.Flag;

    public DateTime? RentalStartDateUtc => _orderItem.RentalStartDateUtc;

    public DateTime? RentalEndDateUtc => _orderItem.RentalEndDateUtc;

    public DateTime CreatedOnUtc => _orderItem.CreatedOnUtc;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}