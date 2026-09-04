#nullable enable

using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{ProductReview}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>ProductReview</c> is a plain
///     <see cref="Grand.Domain.Common.BaseEntity" /> with a single <c>StoreId</c> field, not
///     <c>IStoreLinkEntity</c> — same shape family as <c>Order</c>/<c>GiftVoucher</c>.
///
///     Unlike GiftVoucher, ProductReview has no "global" concept: every review is written by a
///     storefront customer against exactly one store, and neither host's original controller nor
///     <c>ProductReviewViewModelService</c> ever treats an empty <c>StoreId</c> as cross-store
///     visible. <see cref="HasAccess"/> and <see cref="CanView"/> are therefore identical — no
///     loose/strict split, unlike Category/Collection/GiftVoucher.
/// </summary>
public class StoreProductReviewDataScope(IContextAccessor contextAccessor) : IAdminDataScope<ProductReview>
{
    public Task<bool> HasAccess(ProductReview entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public Task<bool> CanView(ProductReview entity) => HasAccess(entity);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => false;
    public string? DefaultVendorId => null;
    // Unused by this entity, kept only for IAdminDataScope<TEntity> interface conformance.
    public bool CanFeatureOnHomepage => true;
}
