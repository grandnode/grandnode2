using DotLiquid;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidMerchandiseReturn : Drop
{
    private readonly DomainHost _host;
    private readonly MerchandiseReturn _merchandiseReturn;

    private readonly MerchandiseReturnNote _merchandiseReturnNote;
    private readonly Order _order;
    private readonly Store _store;

    private readonly string url;

    public LiquidMerchandiseReturn(MerchandiseReturn merchandiseReturn, Store store, DomainHost host,
        Order order, MerchandiseReturnNote merchandiseReturnNote = null)
    {
        _merchandiseReturn = merchandiseReturn;
        _order = order;
        _store = store;
        _host = host;
        _merchandiseReturnNote = merchandiseReturnNote;

        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        Items = new List<LiquidMerchandiseReturnItem>();
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Id => _merchandiseReturn.Id;

    public int ReturnNumber => _merchandiseReturn.ReturnNumber;

    public string ExternalId => _merchandiseReturn.ExternalId;

    public string OrderId => _order.Id;

    public string OrderNumber => _order.OrderNumber.ToString();

    public string OrderCode => _order.Code;

    public string CustomerComment => FormatText.ConvertText(_merchandiseReturn.CustomerComments);

    public string StaffNotes => FormatText.ConvertText(_merchandiseReturn.StaffNotes);

    public string Status { get; set; }

    public ICollection<LiquidMerchandiseReturnItem> Items { get; }

    public DateTime CreatedOnUtc => _merchandiseReturn.CreatedOnUtc;

    public string PickupDate => _merchandiseReturn.PickupDate.ToShortDateString();

    public DateTime PickupDateUtc => _merchandiseReturn.PickupDate;

    public string PickupAddressFirstName => _merchandiseReturn.PickupAddress.FirstName;

    public string PickupAddressLastName => _merchandiseReturn.PickupAddress.LastName;

    public string PickupAddressPhoneNumber => _merchandiseReturn.PickupAddress.PhoneNumber;

    public string PickupAddressEmail => _merchandiseReturn.PickupAddress.Email;

    public string PickupAddressFaxNumber => _merchandiseReturn.PickupAddress.FaxNumber;

    public string PickupAddressCompany => _merchandiseReturn.PickupAddress.Company;

    public string PickupAddressVatNumber => _merchandiseReturn.PickupAddress.VatNumber;

    public string PickupAddressAddress1 => _merchandiseReturn.PickupAddress.Address1;

    public string PickupAddressAddress2 => _merchandiseReturn.PickupAddress.Address2;

    public string PickupAddressCity => _merchandiseReturn.PickupAddress.City;

    public string PickupAddressStateProvince { get; set; }

    public string PickupAddressZipPostalCode => _merchandiseReturn.PickupAddress.ZipPostalCode;

    public string PickupAddressCountry { get; set; }

    public string NewNoteText => FormatText.ConvertText(_merchandiseReturnNote?.Note);

    public string MerchandiseReturnNoteAttachmentUrl =>
        $"{url}/download/merchandisereturnnotefile/{_merchandiseReturnNote?.Id}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}