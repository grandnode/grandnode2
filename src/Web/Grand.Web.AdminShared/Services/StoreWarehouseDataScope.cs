#nullable enable

using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{Warehouse}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>Warehouse</c> is a plain <see cref="Grand.Domain.BaseEntity" />
///     with a single <c>StoreId</c> field, not <c>IStoreLinkEntity</c>. Mirrors Store's original
///     <c>ShippingController</c>'s <c>warehouse.StoreId != CurrentStoreId</c> check, repeated at every
///     Warehouse action site in that file.
/// </summary>
public class StoreWarehouseDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Warehouse>
{
    public Task<bool> HasAccess(Warehouse entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
