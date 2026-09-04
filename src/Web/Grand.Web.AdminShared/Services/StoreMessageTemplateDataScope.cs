#nullable enable

using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{MessageTemplate}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: although <c>MessageTemplate</c> IS
///     <c>IStoreLinkEntity</c>, ownership uses the stricter, already-established
///     <see cref="AclMappingExtension.AccessToEntityByStore{T}"/> idiom (exact single-store
///     ownership: <c>LimitedToStores &amp;&amp; Stores.Count == 1 &amp;&amp; Stores[0] ==
///     storeId</c>) shared with the attribute family (CheckoutAttribute/ContactAttribute/
///     CustomerAttribute/AddressAttribute — see <c>project_store_attributes</c> memory), not the
///     generic scope's plain <c>Stores.Contains</c> check.
///
///     A template can also be shared across N&gt;1 stores without being exclusively owned by any
///     of them — visible/openable read-only, not mutable. <see cref="CanView"/> is therefore
///     looser than <see cref="HasAccess"/>: any membership in <c>Stores</c>, or fully global
///     (<c>!LimitedToStores</c>) — the same loose/strict split as
///     <see cref="StoreGiftVoucherDataScope"/>, just keyed off <c>Stores.Contains</c> instead of
///     an empty-<c>StoreId</c> sentinel.
/// </summary>
public class StoreMessageTemplateDataScope(IContextAccessor contextAccessor) : IAdminDataScope<MessageTemplate>
{
    public Task<bool> HasAccess(MessageTemplate entity) =>
        Task.FromResult(entity is not null &&
            entity.AccessToEntityByStore(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId));

    public Task<bool> CanView(MessageTemplate entity) =>
        Task.FromResult(entity is not null &&
            (!entity.LimitedToStores ||
             entity.Stores.Contains(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)));

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => false; // Store never shows a Stores picker — always self-assigned
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
