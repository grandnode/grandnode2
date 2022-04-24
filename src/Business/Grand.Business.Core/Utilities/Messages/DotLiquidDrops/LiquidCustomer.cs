using DotLiquid;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidCustomer : Drop
    {
        private readonly Customer _customer;
        private readonly CustomerNote _customerNote;
        private readonly Store _store;
        private readonly DomainHost _host;
        private readonly string url;
        public LiquidCustomer(Customer customer, Store store, DomainHost host, CustomerNote customerNote = null)
        {
            _customer = customer;
            _customerNote = customerNote;
            _store = store;
            _host = host;
            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));
            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Email
        {
            get { return _customer.Email; }
        }

        public string Username
        {
            get { return _customer.Username; }
        }

        public string FullName
        {
            get { return _customer.GetFullName(); }
        }

        public string FirstName
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName); }
        }

        public string LastName
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName); }
        }

        public string Gender
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender); }
        }

        public string DateOfBirth
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.DateOfBirth); }
        }

        public string Company
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company); }
        }

        public string StreetAddress
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress); }
        }

        public string StreetAddress2
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2); }
        }

        public string ZipPostalCode
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode); }
        }

        public string City
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City); }
        }

        public string Phone
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone); }
        }

        public string Fax
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax); }
        }

        public string VatNumber
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber); }
        }

        public string VatNumberStatus
        {
            get { return ((VatNumberStatus)_customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId)).ToString(); }
        }

        public string PasswordRecoveryURL
        {
            get { return string.Format("{0}/passwordrecovery/confirm?token={1}&email={2}", url, _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken), WebUtility.UrlEncode(_customer.Email)); }
        }

        public string AccountActivationURL
        {
            get { return string.Format("{0}/account/activation?token={1}&email={2}", url, _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AccountActivationToken), WebUtility.UrlEncode(_customer.Email)); ; }
        }

        public string WishlistURLForCustomer
        {
            get { return string.Format("{0}/wishlist/{1}", url, _customer.CustomerGuid); }
        }

        public string NewNoteText
        {
            get { return FormatText.ConvertText(_customerNote.Note); }
        }

        public string NewTitleText
        {
            get { return _customerNote.Title; }
        }

        public string CustomerNoteAttachmentUrl
        {
            get { return string.Format("{0}/download/customernotefile/{1}", url, _customerNote.Id); }
        }

        public string Token
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode); }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}