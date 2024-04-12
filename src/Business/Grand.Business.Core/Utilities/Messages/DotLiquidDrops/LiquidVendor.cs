using DotLiquid;
using Grand.Domain.Vendors;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidVendor : Drop
{
    private readonly Vendor _vendor;

    public LiquidVendor(Vendor vendor)
    {
        _vendor = vendor;
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Name => _vendor.Name;

    public string Email => _vendor.Email;

    public string Description => _vendor.Description;

    public string Address1 => _vendor.Address?.Address1;

    public string Address2 => _vendor.Address?.Address2;

    public string City => _vendor.Address?.City;

    public string Company => _vendor.Address?.Company;

    public string FaxNumber => _vendor.Address?.FaxNumber;

    public string PhoneNumber => _vendor.Address?.PhoneNumber;

    public string ZipPostalCode => _vendor.Address?.ZipPostalCode;

    public string StateProvince { get; set; }

    public string Country { get; set; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}