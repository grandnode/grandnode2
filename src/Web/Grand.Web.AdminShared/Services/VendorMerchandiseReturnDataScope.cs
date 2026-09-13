#nullable enable

using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Vendor's <see cref="IAdminDataScope{MerchandiseReturn}" />. Bespoke: ownership is a flat
///     <c>VendorId</c> field directly on the entity — simpler than Order's child-collection
///     ownership, closer to Shipment's shape. Reimplements the equivalent of
///     <c>Grand.Web.Vendor/Extensions/HasAccess.cs</c>'s <c>HasAccessToMerchandiseReturn</c> rather
///     than calling it: <c>Grand.Web.AdminShared</c> has no project reference to
///     <c>Grand.Web.Vendor</c> (the reference direction is Vendor→AdminShared), the same constraint
///     <see cref="VendorOrderDataScope" />/<see cref="VendorShipmentDataScope" /> already work
///     around. Once every controller call site is migrated onto <c>scope.HasAccess</c>,
///     <c>HasAccessToMerchandiseReturn</c> has no remaining callers and is deleted (Task 9).
/// </summary>
public class VendorMerchandiseReturnDataScope(IContextAccessor contextAccessor)
    : IAdminDataScope<MerchandiseReturn>
{
    public Task<bool> HasAccess(MerchandiseReturn entity) =>
        Task.FromResult(entity is not null &&
            entity.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Vendor";
    public bool ShowStoreSelector => false;
    public string? DefaultVendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool CanFeatureOnHomepage => false;
}
