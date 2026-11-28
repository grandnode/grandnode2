using DotLiquid;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidOrder : Drop
{
    private readonly Currency? _currency;
    private readonly Customer? _customer;
    private readonly DomainHost? _host;
    private readonly Language? _language;
    private readonly Order _order;
    private readonly OrderNote? _orderNote;
    private readonly Store _store;
    private readonly Vendor? _vendor;

    private readonly string url;

    public LiquidOrder(Order order, Customer? customer, Language? language, Currency? currency, Store store,
        DomainHost? host, OrderNote? orderNote = null, Vendor? vendor = null)
    {
        _order = order;
        _customer = customer;
        _language = language;
        _orderNote = orderNote;
        _currency = currency;
        _store = store;
        _vendor = vendor;
        _host = host;

        url = _host?.Url.Trim('/') ?? _store.Url.Trim('/');


        OrderItems = new List<LiquidOrderItem>();
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string OrderNumber => _order.OrderNumber.ToString();

    public string? OrderCode => _order.Code;

    public string PurchaseOrderNumber => WebUtility.HtmlEncode(_order.PurchaseOrderNumber ?? "");

    //the due date is a calendar date, so it is shown as stored, without a time zone shift
    public string PaymentDueDate => _order.PaymentDueDateUtc?.ToString("D") ?? "";

    public string CustomerFullName => WebUtility.HtmlEncode($"{_order.BillingAddress?.FirstName} {_order.BillingAddress?.LastName}");

    public string? CustomerEmail => WebUtility.HtmlEncode(_order.BillingAddress?.Email);

    public string? BillingAddressName => WebUtility.HtmlEncode(_order.BillingAddress?.Name);

    public string? BillingFirstName => WebUtility.HtmlEncode(_order.BillingAddress?.FirstName);

    public string? BillingLastName => WebUtility.HtmlEncode(_order.BillingAddress?.LastName);

    public string? BillingPhoneNumber => WebUtility.HtmlEncode(_order.BillingAddress?.PhoneNumber);

    public string? BillingEmail => WebUtility.HtmlEncode(_order.BillingAddress?.Email);

    public string? BillingFaxNumber => WebUtility.HtmlEncode(_order.BillingAddress?.FaxNumber);

    public string? BillingCompany => WebUtility.HtmlEncode(_order.BillingAddress?.Company);

    public string? BillingVatNumber => WebUtility.HtmlEncode(_order.BillingAddress?.VatNumber);

    public string? BillingAddress1 => WebUtility.HtmlEncode(_order.BillingAddress?.Address1);

    public string? BillingAddress2 => WebUtility.HtmlEncode(_order.BillingAddress?.Address2);

    public string? BillingCity => WebUtility.HtmlEncode(_order.BillingAddress?.City);

    public string? BillingStateProvince { get; set; }

    public string? BillingZipPostalCode => WebUtility.HtmlEncode(_order.BillingAddress?.ZipPostalCode);

    public string? BillingCountry { get; set; }

    public string? BillingCustomAttributes { get; set; }

    public string? ShippingMethod => _order.ShippingMethod;

    public string? ShippingAdditionDescription => _order.ShippingOptionAttributeDescription;

    public string? ShippingAddressName => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.Name : "");

    public string? ShippingFirstName => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.FirstName : "");

    public string? ShippingLastName => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.LastName : "");

    public string? ShippingPhoneNumber => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.PhoneNumber : "");

    public string? ShippingEmail => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.Email : "");

    public string? ShippingFaxNumber => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.FaxNumber : "");

    public string? ShippingCompany => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.Company : "");

    public string? ShippingAddress1 => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.Address1 : "");

    public string? ShippingAddress2 => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.Address2 : "");

    public string? ShippingCity => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.City : "");

    public string? ShippingStateProvince { get; set; }

    public string? ShippingZipPostalCode => WebUtility.HtmlEncode(_order.ShippingAddress != null ? _order.ShippingAddress.ZipPostalCode : "");

    public string? ShippingCountry { get; set; }

    public string? ShippingCustomAttributes { get; set; }

    public string? PaymentMethod { get; set; }


    public string? VatNumber => WebUtility.HtmlEncode(_order.VatNumber);

    public string CreatedOn => _order.CreatedOnUtc.ToLocalTime().ToString("D");

    public DateTime CreatedOnUtc => _order.CreatedOnUtc;

    public string OrderURLForCustomer => $"{url}/orderdetails/{_order.Id}";

    public double PaidAmount => _order.PaidAmount;

    public double RefundedAmount => _order.RefundedAmount;

    public string NewNoteText => FormatText.ConvertText(_orderNote?.Note);

    public string OrderNoteAttachmentUrl => $"{url}/download/ordernotefile/{_orderNote?.Id}";

    public string? VendorName => WebUtility.HtmlEncode(_vendor?.Name);

    public string? VendorEmail => WebUtility.HtmlEncode(_vendor?.Email);

    public ICollection<LiquidOrderItem> OrderItems { get; }

    public bool DisplaySubTotalDiscount { get; set; }

    public string? SubTotalDiscount { get; set; }

    public string? SubTotal { get; set; }

    public string? Shipping { get; set; }

    public string? Tax { get; set; }

    public string? Total { get; set; }

    public bool DisplayShipping => _order.ShippingStatusId != ShippingStatus.ShippingNotRequired;

    public bool DisplayPaymentMethodFee => _order.PaymentMethodAdditionalFeeExclTax > 0;

    public string? PaymentMethodAdditionalFee { get; set; }

    public bool DisplayTax { get; set; }

    public bool DisplayTaxRates { get; set; }

    public Dictionary<string, string>? TaxRates { get; set; }

    public bool DisplayDiscount { get; set; }

    public string? Discount { get; set; }

    public string? CheckoutAttributeDescription => _order.CheckoutAttributeDescription;

    public Dictionary<string, string>? GiftVouchers { get; set; }

    public bool RedeemedLoyaltyPointsEntryExists => _order.RedeemedLoyaltyPoints > 0;

    public string? RPAmount { get; set; }
    public string? RPPoints { get; set; }

    public bool IsRecurring => _order.IsRecurring;

    public int RecurringCycleLength => _order.RecurringCycleLength;

    public int RecurringCyclePeriodId => (int)_order.RecurringCyclePeriodId;

    public int RecurringTotalCycles => _order.RecurringTotalCycles;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}