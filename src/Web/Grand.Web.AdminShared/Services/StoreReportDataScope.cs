#nullable enable

using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IReportDataScope" />. Forces every report query to the current staff
///     store; never a vendor concept. No store/vendor picker (Store is implicitly scoped, same
///     shape as every prior phase's Store scope). Reuses Admin's resource keys ("Admin" prefix) —
///     Store has no distinct Reports resource set today.
/// </summary>
public class StoreReportDataScope(IContextAccessor contextAccessor) : IReportDataScope
{
    public string StoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string VendorId => "";
    public bool ShowStoreSelector => false;
    public bool ShowVendorSelector => false;
    public string ResourceKeyPrefix => "Admin";
    public bool CanIncludeProduct(Product product) => true;
}
