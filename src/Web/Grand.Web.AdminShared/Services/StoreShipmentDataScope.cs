#nullable enable

using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{Shipment}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>Shipment</c> is a plain <see cref="Grand.Domain.BaseEntity" />
///     with a single <c>StoreId</c> field, not <c>IStoreLinkEntity</c> (no <c>Stores</c>/
///     <c>LimitedToStores</c> list), so the generic class's <c>where TEntity : BaseEntity,
///     IStoreLinkEntity</c> constraint doesn't apply. Mirrors Store's original controller's
///     <c>shipment.StoreId != StaffStoreId</c> check, repeated at every action site in that file.
///     No <see cref="CanView" /> override: Store's original code has one uniform check for both
///     viewing and mutating, unlike Category/Product's loose/strict split.
/// </summary>
public class StoreShipmentDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Shipment>
{
    public Task<bool> HasAccess(Shipment entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for Shipment; required interface member
}
