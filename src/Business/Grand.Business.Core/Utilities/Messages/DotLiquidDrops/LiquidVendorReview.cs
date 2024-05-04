using DotLiquid;
using Grand.Domain.Vendors;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidVendorReview : Drop
{
    private readonly Vendor _vendor;
    private readonly VendorReview _vendorReview;

    public LiquidVendorReview(Vendor vendor, VendorReview vendorReview)
    {
        _vendorReview = vendorReview;
        _vendor = vendor;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string VendorName => _vendor.Name;

    public string Title => _vendorReview.Title;

    public string ReviewText => _vendorReview.ReviewText;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}