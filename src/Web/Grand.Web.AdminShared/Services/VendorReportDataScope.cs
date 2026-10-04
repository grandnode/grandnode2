#nullable enable

using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;
public class VendorReportDataScope(IContextAccessor contextAccessor) : IReportDataScope
{
    public string StoreId => "";
    public string VendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool ShowStoreSelector => false;
    public bool ShowVendorSelector => false;
    public string ResourceKeyPrefix => "Vendor";

    public bool CanIncludeProduct(Product? product) =>
        product is not null && product.VendorId == contextAccessor.WorkContext.CurrentVendor.Id;
}
