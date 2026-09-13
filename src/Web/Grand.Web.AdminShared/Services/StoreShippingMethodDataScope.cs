#nullable enable

using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{ShippingMethod}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>ShippingMethod</c> is a plain
///     <see cref="Grand.Domain.BaseEntity" /> with a single <c>StoreId</c> field, not
///     <c>IStoreLinkEntity</c>. Mirrors Store's original <c>ShippingController</c>'s
///     <c>sm.StoreId != CurrentStoreId</c> check.
/// </summary>
public class StoreShippingMethodDataScope(IContextAccessor contextAccessor) : IAdminDataScope<ShippingMethod>
{
    public Task<bool> HasAccess(ShippingMethod entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
