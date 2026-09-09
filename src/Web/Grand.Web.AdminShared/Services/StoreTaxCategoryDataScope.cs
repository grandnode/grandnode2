#nullable enable

using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{TaxCategory}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>TaxCategory</c> is a plain
///     <c>BaseEntity</c> with a flat <c>StoreId</c> string (empty means global/shared), not
///     <c>IStoreLinkEntity</c> — ownership is a single exact-match comparison, not a
///     <c>Stores.Contains</c> list check. Same shape as <see cref="StoreOrderDataScope"/>/
///     <see cref="StoreEmailAccountDataScope"/>.
/// </summary>
public class StoreTaxCategoryDataScope(IContextAccessor contextAccessor) : IAdminDataScope<TaxCategory>
{
    public Task<bool> HasAccess(TaxCategory entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => false; // Store's grid never shows an editable store column
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
