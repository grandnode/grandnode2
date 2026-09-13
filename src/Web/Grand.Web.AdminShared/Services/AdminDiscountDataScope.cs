#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IAdminDataScope{Discount}" />. Deliberately NOT the generic
///     <see cref="GlobalAdminDataScope{TEntity}" /> — Admin's original DiscountController gates
///     Create/Edit/Delete on <c>groupService.IsStoreManager(CurrentCustomer)</c>, a customer-group
///     flag independent of area/host, that Store's original controller never checked (Store always
///     applies the strict store check unconditionally). Reusing the always-true generic scope here
///     would silently drop this restriction for the rare case of an Admin-area user who is also a
///     store manager. Same class of Admin-side scope logic as
///     <see cref="Grand.Web.AdminShared.Services.AdminOrderDataScope"/> (there: IsSalesManager/SeId;
///     here: IsStoreManager/StaffStoreId). See ARCH-001 Discount consolidation spec §2.
/// </summary>
public class AdminDiscountDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<Discount>
{
    public async Task<bool> HasAccess(Discount entity)
    {
        if (entity is null) return false;
        var isStoreManager = await groupService.IsStoreManager(contextAccessor.WorkContext.CurrentCustomer);
        return !isStoreManager ||
               entity.AccessToEntityByStore(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);
    }

    public async Task<bool> CanView(Discount entity)
    {
        if (entity is null) return false;
        var isStoreManager = await groupService.IsStoreManager(contextAccessor.WorkContext.CurrentCustomer);
        if (!isStoreManager) return true;

        //looser than HasAccess: viewing a global or multi-store discount is allowed (with a
        //warning, applied by the caller) for a store manager; only mutating one they don't
        //exclusively own is denied
        if (!entity.LimitedToStores ||
            (entity.Stores.Contains(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) &&
             entity.Stores.Count > 1))
            return true;

        return entity.AccessToEntityByStore(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);
    }

    // The synchronous DefaultStoreId required by IAdminDataScope<TEntity> has no sync-safe
    // implementation here: it depends on IsStoreManager, which is async, and this codebase forbids
    // blocking on a Task (`.GetAwaiter().GetResult()`) in request/service code - see
    // `.ai/constraints.md` "Never block on a Task". Unlike AdminOrderDataScope's DefaultStoreId
    // (hardcoded null, no I/O needed), this scope genuinely cannot answer synchronously. Every call
    // site (BaseDiscountController) is already async, so it uses GetDefaultStoreIdAsync below
    // instead; this member throws rather than silently blocking or returning a wrong answer.
    public string? DefaultStoreId =>
        throw new NotSupportedException(
            $"{nameof(AdminDiscountDataScope)}.{nameof(DefaultStoreId)} requires an async customer-group " +
            $"check and has no sync-safe implementation. Use {nameof(GetDefaultStoreIdAsync)} instead.");

    public async Task<string?> GetDefaultStoreIdAsync()
    {
        var isStoreManager = await groupService.IsStoreManager(contextAccessor.WorkContext.CurrentCustomer);
        return isStoreManager ? contextAccessor.WorkContext.CurrentCustomer.StaffStoreId : null;
    }

    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for Discount; required interface member
}
