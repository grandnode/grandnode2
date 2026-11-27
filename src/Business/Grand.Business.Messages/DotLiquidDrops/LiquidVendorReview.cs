using DotLiquid;
using Grand.Domain.Vendors;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

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

    public string? VendorName => WebUtility.HtmlEncode(_vendor.Name);

    public string? Title => WebUtility.HtmlEncode(_vendorReview.Title);

    public string? ReviewText => WebUtility.HtmlEncode(_vendorReview.ReviewText);

    public IDictionary<string, string> AdditionalTokens { get; set; }
}