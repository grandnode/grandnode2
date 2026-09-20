#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;
public class AdminReportDataScope : IReportDataScope
{
    public string StoreId => "";
    public string VendorId => "";
    public bool ShowStoreSelector => true;
    public bool ShowVendorSelector => true;
    public string ResourceKeyPrefix => "Admin";
    public bool CanIncludeProduct(Product? product) => true;
}
