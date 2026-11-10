using DotLiquid;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using System.Globalization;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidShipmentItem : Drop
{
    private readonly Language _language;
    private readonly Order _order;
    private readonly OrderItem _orderItem;
    private readonly Product _product;
    private readonly Shipment _shipment;
    private readonly ShipmentItem _shipmentItem;

    public LiquidShipmentItem(ShipmentItem shipmentItem, Shipment shipment, Order order, OrderItem orderItem,
        Product product, Language language)
    {
        _shipmentItem = shipmentItem;
        _language = language;
        _shipment = shipment;
        _order = order;
        _orderItem = orderItem;
        _product = product;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public bool ShowSkuOnProductDetailsPage { get; set; }

    public string ProductName {
        get {
            var name = "";
            if (_product != null)
                name = WebUtility.HtmlEncode(_product.Name);

            return name;
        }
    }

    public string ProductSku => _orderItem.Sku;

    public string AttributeDescription {
        get {
            var attDesc = "";

            if (_orderItem != null)
                attDesc = _orderItem.AttributeDescription;

            return attDesc;
        }
    }

    public string ShipmentId => _shipment.Id;

    public string OrderItemId => _shipmentItem.OrderItemId;

    public string ProductId => _shipmentItem.ProductId;

    public int Quantity => _shipmentItem.Quantity;

    public string WarehouseId => _shipmentItem.WarehouseId;

    /// <summary>
    ///     The serial numbers shipped, comma separated; empty for a product tracking none
    /// </summary>
    public string SerialNumbers =>
        WebUtility.HtmlEncode(string.Join(", ", _shipmentItem.SerialNumbers ?? []));

    /// <summary>
    ///     The batches shipped with their quantities, as "L1 x 2, L2 x 1"; empty for a product tracking none
    /// </summary>
    public string Batches =>
        WebUtility.HtmlEncode(string.Join(", ", (_shipmentItem.Batches ?? []).Select(x => $"{x.Number} x {x.Quantity}")));

    /// <summary>
    ///     The end of the warranty of the units shipped, yyyy-MM-dd; empty without one
    /// </summary>
    public string WarrantyUntil =>
        _shipmentItem.WarrantyUntilUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}