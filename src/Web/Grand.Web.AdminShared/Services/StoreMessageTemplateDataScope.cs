#nullable enable

using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{MessageTemplate}" />. Similar to
///     <see cref="StoreAdminDataScope{TEntity}" /> (it also uses the strict
///     <see cref="AclMappingExtension.AccessToEntityByStore{T}"/> ownership check), but bespoke
///     because Store never exposes a Stores picker for message templates (always self-assigned),
///     so <see cref="ShowStoreSelector"/> must be <c>false</c>.
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
