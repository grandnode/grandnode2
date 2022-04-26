using DotLiquid;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidMerchandiseReturn : Drop
    {
        private readonly MerchandiseReturn _merchandiseReturn;
        private readonly Order _order;
        private readonly Store _store;
        private readonly DomainHost _host;

        private MerchandiseReturnNote _merchandiseReturnNote;
        private ICollection<LiquidMerchandiseReturnItem> _items;

        private string url;

        public LiquidMerchandiseReturn(MerchandiseReturn merchandiseReturn, Store store, DomainHost host,
            Order order, MerchandiseReturnNote merchandiseReturnNote = null)
        {
            _merchandiseReturn = merchandiseReturn;
            _order = order;
            _store = store;
            _host = host;
            _merchandiseReturnNote = merchandiseReturnNote;

            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

            _items = new List<LiquidMerchandiseReturnItem>();
            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Id
        {
            get { return _merchandiseReturn.Id; }
        }

        public int ReturnNumber
        {
            get { return _merchandiseReturn.ReturnNumber; }
        }

        public string ExternalId
        {
            get { return _merchandiseReturn.ExternalId; }
        }

        public string OrderId
        {
            get { return _order.Id; }
        }
        public string OrderNumber
        {
            get { return _order.OrderNumber.ToString(); }
        }

        public string OrderCode
        {
            get { return _order.Code; }
        }

        public string CustomerComment
        {
            get { return FormatText.ConvertText(_merchandiseReturn.CustomerComments); }
        }

        public string StaffNotes
        {
            get { return FormatText.ConvertText(_merchandiseReturn.StaffNotes); }
        }

        public string Status { get; set; }

        public ICollection<LiquidMerchandiseReturnItem> Items
        {
            get
            {
                return _items;
            }
        }

        public DateTime CreatedOnUtc
        {
            get { return _merchandiseReturn.CreatedOnUtc; }
        }

        public string PickupDate
        {
            get { return _merchandiseReturn.PickupDate.ToShortDateString(); }
        }
        public DateTime PickupDateUtc
        {
            get { return _merchandiseReturn.PickupDate; }
        }

        public string PickupAddressFirstName
        {
            get { return _merchandiseReturn.PickupAddress.FirstName; }
        }

        public string PickupAddressLastName
        {
            get { return _merchandiseReturn.PickupAddress.LastName; }
        }

        public string PickupAddressPhoneNumber
        {
            get { return _merchandiseReturn.PickupAddress.PhoneNumber; }
        }

        public string PickupAddressEmail
        {
            get { return _merchandiseReturn.PickupAddress.Email; }
        }

        public string PickupAddressFaxNumber
        {
            get { return _merchandiseReturn.PickupAddress.FaxNumber; }
        }

        public string PickupAddressCompany
        {
            get { return _merchandiseReturn.PickupAddress.Company; }
        }

        public string PickupAddressVatNumber
        {
            get { return _merchandiseReturn.PickupAddress.VatNumber; }
        }

        public string PickupAddressAddress1
        {
            get { return _merchandiseReturn.PickupAddress.Address1; }
        }

        public string PickupAddressAddress2
        {
            get { return _merchandiseReturn.PickupAddress.Address2; }
        }

        public string PickupAddressCity
        {
            get { return _merchandiseReturn.PickupAddress.City; }
        }

        public string PickupAddressStateProvince { get; set; }

        public string PickupAddressZipPostalCode
        {
            get { return _merchandiseReturn.PickupAddress.ZipPostalCode; }
        }

        public string PickupAddressCountry { get; set; }

        public string NewNoteText
        {
            get { return FormatText.ConvertText(_merchandiseReturnNote?.Note); }
        }

        public string MerchandiseReturnNoteAttachmentUrl
        {
            get
            {
                return string.Format("{0}/download/merchandisereturnnotefile/{1}", url, _merchandiseReturnNote?.Id);
            }
        }
        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}