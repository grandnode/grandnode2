#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IReportDataScope" />. Admin's report screens accept an explicit
///     storeId/vendorId filter from the posted grid model itself (or default to "" = unscoped) — the
///     scope object's job for Admin is purely to advertise the *capability* to filter
///     (<see cref="ShowStoreSelector" />/<see cref="ShowVendorSelector" /> = true), not to supply a
///     forced value the way Store/Vendor's scopes do. See ARCH-001 Reports consolidation spec §3.
/// </summary>
public class AdminReportDataScope : IReportDataScope
{
    public string StoreId => "";
    public string VendorId => "";
    public bool ShowStoreSelector => true;
    public bool ShowVendorSelector => true;
    public string ResourceKeyPrefix => "Admin";
    public bool CanIncludeProduct(Product product) => true;
}
