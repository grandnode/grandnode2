#nullable enable

using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Vendor's <see cref="IReportDataScope" />. Forces every report query to the current vendor;
///     never a store concept on these screens. <see cref="CanIncludeProduct" /> reimplements the
///     equivalent of <c>Grand.Web.Vendor/Extensions/HasAccess.cs</c>'s
///     <c>HasAccessToProduct(Product)</c> rather than calling it: <c>Grand.Web.AdminShared</c> has no
///     project reference to <c>Grand.Web.Vendor</c> (the reference direction is Vendor→AdminShared),
///     same constraint <c>VendorMerchandiseReturnDataScope</c>/<c>VendorOrderDataScope</c> already
///     work around. Preserves Vendor's existing <c>BestsellersReportList</c> product-ownership
///     post-filter verbatim (ARCH-001 Reports consolidation spec §2.3/§9).
/// </summary>
public class VendorReportDataScope(IContextAccessor contextAccessor) : IReportDataScope
{
    public string StoreId => "";
    public string VendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool ShowStoreSelector => false;
    public bool ShowVendorSelector => false;
    public string ResourceKeyPrefix => "Vendor";

    public bool CanIncludeProduct(Product product) =>
        product is not null && product.VendorId == contextAccessor.WorkContext.CurrentVendor.Id;
}
