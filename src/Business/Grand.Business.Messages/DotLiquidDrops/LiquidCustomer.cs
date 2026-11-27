using DotLiquid;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidCustomer : Drop
{
    private readonly Customer _customer;
    private readonly CustomerNote? _customerNote;
    private readonly DomainHost? _host;
    private readonly Store _store;
    private readonly string url;

    public LiquidCustomer(Customer customer, Store store, DomainHost host, CustomerNote? customerNote = null)
    {
        _customer = customer;
        _customerNote = customerNote;
        _store = store;
        _host = host;
        url = _host?.Url.Trim('/') ?? _store.Url.Trim('/');
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string? Email => WebUtility.HtmlEncode(_customer.Email);

    public string? Username => WebUtility.HtmlEncode(_customer.Username);

    public string FullName => WebUtility.HtmlEncode(_customer.GetFullName());

    public string? FirstName => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName));

    public string? LastName => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName));

    public string? Gender => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender));

    public string? DateOfBirth => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.DateOfBirth));

    public string? Company => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company));

    public string? StreetAddress => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress));

    public string? StreetAddress2 => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2));

    public string? ZipPostalCode => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode));

    public string? City => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City));

    public string? Phone => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone));

    public string? Fax => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax));

    public string? VatNumber => WebUtility.HtmlEncode(_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber));

    public string VatNumberStatus =>
        ((VatNumberStatus)_customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId)).ToString();

    //a recovery requested from a panel's sign-in screen is confirmed in that panel
    public string PasswordRecoveryURL =>
        $"{url}{PasswordRecoveryAreaPath}/passwordrecovery/confirm?token={_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryToken)}&email={WebUtility.UrlEncode(_customer.Email)}";

    private string PasswordRecoveryAreaPath
    {
        get {
            var area = _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.PasswordRecoveryArea);
            return string.IsNullOrEmpty(area) ? "" : $"/{area}";
        }
    }

    public string AccountActivationURL =>
        $"{url}/account/activation?token={_customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AccountActivationToken)}&email={WebUtility.UrlEncode(_customer.Email)}";

    public string WishlistURLForCustomer => $"{url}/wishlist/{_customer.CustomerGuid}";

    public string NewNoteText => FormatText.ConvertText(_customerNote?.Note);

    public string? NewTitleText => WebUtility.HtmlEncode(_customerNote?.Title);

    public string CustomerNoteAttachmentUrl => $"{url}/download/customernotefile/{_customerNote?.Id}";

    public string? Token => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorValidCode);

    public IDictionary<string, string> AdditionalTokens { get; set; }
}