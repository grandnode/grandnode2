using DotLiquid;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidMerchandiseReturn : Drop
    {
        private MerchandiseReturn _merchandiseReturn;
        private Order _order;
        private Store _store;
        private MerchandiseReturnNote _merchandiseReturnNote;
        private ICollection<LiquidMerchandiseReturnItem> _items;

        public LiquidMerchandiseReturn(MerchandiseReturn merchandiseReturn, Store store, Order order, MerchandiseReturnNote merchandiseReturnNote = null)
        {
            _merchandiseReturn = merchandiseReturn;
            _order = order;
            _store = store;
            _merchandiseReturnNote = merchandiseReturnNote;
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
                return string.Format("{0}/download/merchandisereturnnotefile/{1}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _merchandiseReturnNote?.Id);
            }
        }
        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}