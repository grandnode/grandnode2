using DotLiquid;
using Grand.Domain.Vendors;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidVendor : Drop
{
    private readonly Vendor _vendor;

    public LiquidVendor(Vendor vendor)
    {
        _vendor = vendor;
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Name => WebUtility.HtmlEncode(_vendor.Name);

    public string Email => WebUtility.HtmlEncode(_vendor.Email);

    public string Description => _vendor.Description;

    public string Address1 => WebUtility.HtmlEncode(_vendor.Address?.Address1);

    public string Address2 => WebUtility.HtmlEncode(_vendor.Address?.Address2);

    public string City => WebUtility.HtmlEncode(_vendor.Address?.City);

    public string Company => WebUtility.HtmlEncode(_vendor.Address?.Company);

    public string FaxNumber => WebUtility.HtmlEncode(_vendor.Address?.FaxNumber);

    public string PhoneNumber => WebUtility.HtmlEncode(_vendor.Address?.PhoneNumber);

    public string ZipPostalCode => WebUtility.HtmlEncode(_vendor.Address?.ZipPostalCode);

    public string StateProvince { get; set; }

    public string Country { get; set; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}