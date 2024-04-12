using DotLiquid;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidCustomer : Drop
{
    private readonly Customer _customer;
    private readonly CustomerNote _customerNote;
    private readonly DomainHost _host;
    private readonly Store _store;
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

    public string Email => _customer.Email;

    public string Username => _customer.Username;

    public string FullName => _customer.GetFullName();

    public string FirstName => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);

    public string LastName => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);

    public string Gender => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender);

    public string DateOfBirth => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.DateOfBirth);

    public string Company => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company);

    public string StreetAddress => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress);

    public string StreetAddress2 => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2);

    public string ZipPostalCode => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode);

    public string City => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City);

    public string Phone => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);

    public string Fax => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);

    public string VatNumber => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);

    public string VatNumberStatus =>
        ((VatNumberStatus)_customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId)).ToString();

    public string PasswordRecoveryURL =>
        $"{url}/passwordrecovery/confirm?token={_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken)}&email={WebUtility.UrlEncode(_customer.Email)}";

    public string AccountActivationURL =>
        $"{url}/account/activation?token={_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AccountActivationToken)}&email={WebUtility.UrlEncode(_customer.Email)}";

    public string WishlistURLForCustomer => $"{url}/wishlist/{_customer.CustomerGuid}";

    public string NewNoteText => FormatText.ConvertText(_customerNote.Note);

    public string NewTitleText => _customerNote.Title;

    public string CustomerNoteAttachmentUrl => $"{url}/download/customernotefile/{_customerNote.Id}";

    public string Token => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode);

    public IDictionary<string, string> AdditionalTokens { get; set; }
}