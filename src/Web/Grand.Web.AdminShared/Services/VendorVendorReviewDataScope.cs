#nullable enable

using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Vendor's <see cref="IAdminDataScope{VendorReview}" />. Bespoke: ownership is a flat
///     <c>VendorId</c> field directly on the entity, the same shape as
///     <see cref="VendorShipmentDataScope" />/<see cref="VendorOrderDataScope" />'s simpler sibling
///     (no child-collection scan, no <see cref="IAdminDataScope{TEntity}.FilterOrderItems" />
///     override needed). Reimplements the existing
///     <c>Grand.Web.Vendor/Extensions/HasAccess.cs</c>'s <c>HasAccessToVendorReview</c> inline rather
///     than calling it — <c>Grand.Web.AdminShared</c> has no project reference to
///     <c>Grand.Web.Vendor</c> (the reference direction is the other way), the same constraint every
///     prior Vendor-scope class already works around.
/// </summary>
public class VendorVendorReviewDataScope(IContextAccessor contextAccessor) : IAdminDataScope<VendorReview>
{
    public Task<bool> HasAccess(VendorReview entity) =>
        Task.FromResult(entity is not null &&
            !string.IsNullOrEmpty(contextAccessor.WorkContext.CurrentVendor.Id) &&
            entity.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Vendor";
    public bool ShowStoreSelector => false;
    public string? DefaultVendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool CanFeatureOnHomepage => false;
}
