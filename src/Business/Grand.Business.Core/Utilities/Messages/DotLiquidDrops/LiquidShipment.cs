using DotLiquid;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidShipment : Drop
{
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly Order _order;
    private readonly Shipment _shipment;
    private readonly Store _store;
    private readonly string url;


    public LiquidShipment(Shipment shipment, Order order, Store store, DomainHost host, Language language)
    {
        _shipment = shipment;
        _language = language;
        _store = store;
        _order = order;
        _host = host;
        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        ShipmentItems = new List<LiquidShipmentItem>();
        AdditionalTokens = new Dictionary<string, string>();
    }

    public ICollection<LiquidShipmentItem> ShipmentItems { get; }

    public string ShipmentNumber => _shipment.ShipmentNumber.ToString();

    public string TrackingNumber => _shipment.TrackingNumber;

    public DateTime? ShippedDateUtc => _shipment.ShippedDateUtc;

    public DateTime? DeliveryDateUtc => _shipment.DeliveryDateUtc;

    public DateTime CreatedOnUtc => _shipment.CreatedOnUtc;

    public string AdminComment => _shipment.AdminComment;

    public string URLForCustomer => $"{url}/orderdetails/shipment/{_shipment.Id}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}