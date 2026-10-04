#nullable enable

using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{MerchandiseReturn}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>MerchandiseReturn</c> is a plain
///     <see cref="Grand.Domain.BaseEntity" /> with a single <c>StoreId</c> field, not
///     <c>IStoreLinkEntity</c>, so the generic class's <c>where TEntity : BaseEntity,
///     IStoreLinkEntity</c> constraint doesn't apply. Mirrors Store's original controller's
///     <c>merchandiseReturn.StoreId != StaffStoreId</c> check, repeated at every action site in that
///     file. No <see cref="IAdminDataScope{TEntity}.CanView" /> override: Store's original code has
///     one uniform check for both viewing and mutating, no loose/strict split (spec §2.3) — CanView
///     is simply inherited from the interface default, which delegates to <see cref="HasAccess" />.
/// </summary>
public class StoreMerchandiseReturnDataScope(IContextAccessor contextAccessor)
    : IAdminDataScope<MerchandiseReturn>
{
    public Task<bool> HasAccess(MerchandiseReturn entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for this entity; required interface member
}
