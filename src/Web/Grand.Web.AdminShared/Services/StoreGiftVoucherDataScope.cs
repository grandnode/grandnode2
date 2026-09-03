#nullable enable

using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{GiftVoucher}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>GiftVoucher</c> is a plain <see cref="BaseEntity" />
///     with a single <c>StoreId</c> field, not <c>IStoreLinkEntity</c> (no <c>Stores</c>/
///     <c>LimitedToStores</c> list) — same shape family as <c>Order</c>/<see cref="StoreOrderDataScope"/>.
///
///     Unlike Order, GiftVoucher has an implicit "global" concept: an empty/null <c>StoreId</c> is
///     visible from every store per <c>GetGiftVoucherQueryHandler</c>'s
///     <c>gc.StoreId == request.StoreId || gc.StoreId == null || gc.StoreId == ""</c> filter, and
///     Store's original <c>List.cshtml</c> already rendered such vouchers (without an edit link,
///     since the original controller's ownership check denied Edit outright). <see cref="CanView"/>
///     makes that loose visibility explicit so Edit can open it read-only with a warning instead of
///     redirecting away, matching Category/Collection/Product's established split.
/// </summary>
public class StoreGiftVoucherDataScope(IContextAccessor contextAccessor) : IAdminDataScope<GiftVoucher>
{
    public Task<bool> HasAccess(GiftVoucher entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public Task<bool> CanView(GiftVoucher entity) =>
        Task.FromResult(entity is not null &&
            (string.IsNullOrEmpty(entity.StoreId) ||
             entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId));

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
