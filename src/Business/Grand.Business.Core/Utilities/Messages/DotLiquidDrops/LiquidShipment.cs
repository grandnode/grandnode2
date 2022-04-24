using DotLiquid;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidShipment : Drop
    {
        private readonly Shipment _shipment;
        private readonly Order _order;
        private readonly Store _store;
        private readonly Language _language;
        private readonly DomainHost _host;
        private readonly string url;

        private ICollection<LiquidShipmentItem> _shipmentItems;


        public LiquidShipment(Shipment shipment, Order order, Store store, DomainHost host, Language language)
        {
            _shipment = shipment;
            _language = language;
            _store = store;
            _order = order;
            _host = host;
            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

            _shipmentItems = new List<LiquidShipmentItem>();
            AdditionalTokens = new Dictionary<string, string>();
        }

        public ICollection<LiquidShipmentItem> ShipmentItems
        {
            get { return _shipmentItems; }
        }

        public string ShipmentNumber
        {
            get { return _shipment.ShipmentNumber.ToString(); }
        }

        public string TrackingNumber
        {
            get { return _shipment.TrackingNumber; }
        }

        public DateTime? ShippedDateUtc {
            get { return _shipment.ShippedDateUtc; }
        }

        public DateTime? DeliveryDateUtc {
            get { return _shipment.DeliveryDateUtc; }
        }
        public DateTime CreatedOnUtc {
            get { return _shipment.CreatedOnUtc; }
        }

        public string AdminComment
        {
            get { return _shipment.AdminComment; }
        }

        public string URLForCustomer
        {
            get
            {
                return string.Format("{0}/orderdetails/shipment/{1}", url, _shipment.Id);
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}
