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

    // DefaultStoreId is a synchronous property on IAdminDataScope<TEntity>, but IsStoreManager is
    // async and this codebase has no sync-safe path to it (Customer/IGroupService expose no cached
    // sync flag - confirmed by inspection, unlike AdminOrderDataScope's DefaultStoreId which is a
    // hardcoded null and never needed this branch). Blocking on the async call here is a disclosed,
    // pragmatic fallback: ASP.NET Core (Kestrel) has no capturing SynchronizationContext, so this
    // does not carry the classic deadlock risk it would under ASP.NET Framework, but it is still a
    // blocking call on an async check and is otherwise unprecedented in this project (no existing
    // `.GetAwaiter().GetResult()` usage under Grand.Web.AdminShared/Services) - flagged for review.
    public string? DefaultStoreId =>
        DefaultStoreIdAsync().GetAwaiter().GetResult();

    private async Task<string?> DefaultStoreIdAsync()
    {
        var isStoreManager = await groupService.IsStoreManager(contextAccessor.WorkContext.CurrentCustomer);
        return isStoreManager ? contextAccessor.WorkContext.CurrentCustomer.StaffStoreId : null;
    }

    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for Discount; required interface member
}
