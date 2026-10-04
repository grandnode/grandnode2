#nullable enable

using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{EmailAccount}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>EmailAccount</c> is a plain
///     <c>BaseEntity</c> with a flat <c>StoreId</c> string (empty means global/shared), not
///     <c>IStoreLinkEntity</c> — ownership is a single exact-match comparison, not a
///     <c>Stores.Contains</c> list check. No <see cref="CanView"/> override: unlike
///     MessageTemplate/GiftVoucher there is no shared/multi-store-visible case for an email
///     account — <see cref="IEmailAccountService.GetAllEmailAccounts"/> already filters Store's
///     list to an exact <c>StoreId</c> match, so Store never even lists a global account, let
///     alone needs a looser view-only check for one.
/// </summary>
public class StoreEmailAccountDataScope(IContextAccessor contextAccessor) : IAdminDataScope<EmailAccount>
{
    public Task<bool> HasAccess(EmailAccount entity) =>
        Task.FromResult(entity is not null &&
            entity.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => false; // Store never shows a Stores picker — always self-assigned
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
