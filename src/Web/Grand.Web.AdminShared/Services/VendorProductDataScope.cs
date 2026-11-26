#nullable enable

using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class VendorProductDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Product>
{
    public Task<bool> HasAccess(Product entity)
    {
        if (entity is null) return Task.FromResult(false);
        return Task.FromResult(entity.VendorId == contextAccessor.WorkContext.PanelVendor().Id);
    }

    public string? DefaultStoreId => null;

    public string ResourceKeyPrefix => "Vendor";

    public bool ShowStoreSelector => false;

    public string? DefaultVendorId => contextAccessor.WorkContext.PanelVendor().Id;

    public bool CanFeatureOnHomepage => false;
}
