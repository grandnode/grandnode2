#nullable enable

using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{Order}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>Order</c> is a plain <see cref="BaseEntity" />
///     with a single <c>StoreId</c> field, not <c>IStoreLinkEntity</c> (no <c>Stores</c>/
///     <c>LimitedToStores</c> list), so the generic class's <c>where TEntity : BaseEntity,
///     IStoreLinkEntity</c> constraint doesn't apply. Mirrors Store's original controller's
///     <c>order.StoreId != StaffStoreId</c> check, repeated at every action site in that file.
/// </summary>
public class StoreOrderDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Order>
{
    public Task<bool> HasAccess(Order entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
