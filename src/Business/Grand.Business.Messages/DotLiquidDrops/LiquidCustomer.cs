using DotLiquid;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.SharedKernel.Extensions;
using System.Collections.Generic;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidCustomer : Drop
    {
        private Customer _customer;
        private CustomerNote _customerNote;
        private Store _store;

        public LiquidCustomer(Customer customer, Store store, CustomerNote customerNote = null)
        {
            _customer = customer;
            _customerNote = customerNote;
            _store = store;
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
            get { return string.Format("{0}/passwordrecovery/confirm?token={1}&email={2}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken), WebUtility.UrlEncode(_customer.Email)); }
        }

        public string AccountActivationURL
        {
            get { return string.Format("{0}/account/activation?token={1}&email={2}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AccountActivationToken), WebUtility.UrlEncode(_customer.Email)); ; }
        }

        public string WishlistURLForCustomer
        {
            get { return string.Format("{0}/wishlist/{1}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _customer.CustomerGuid); }
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
            get { return string.Format("{0}/download/customernotefile/{1}", (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/')), _customerNote.Id); }
        }

        public string Token
        {
            get { return _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode); }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}