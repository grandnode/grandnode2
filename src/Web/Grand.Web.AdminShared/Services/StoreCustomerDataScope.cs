#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Store's <see cref="IAdminDataScope{Customer}" />. Bespoke, not the generic
///     <see cref="StoreAdminDataScope{TEntity}" />: <c>Customer</c> is a plain <see cref="Grand.Domain.Common.BaseEntity" />
///     with a single <c>StoreId</c> field, not <c>IStoreLinkEntity</c>. Also folds in a business
///     rule with no parallel in any other ARCH-001 entity: the store panel may only manage
///     *registered* customers of its own store (ported from the original controller's
///     <c>GetStoreCustomer</c> helper) — not a scope concept, a deliberate UI/product restriction,
///     but it lives here because every original call site checked it in the same breath as
///     ownership.
/// </summary>
public class StoreCustomerDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<Customer>
{
    public async Task<bool> HasAccess(Customer entity)
    {
        if (entity is null || entity.Deleted) return false;
        if (entity.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return false;
        return await groupService.IsRegistered(entity);
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
