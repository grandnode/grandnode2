#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IAdminDataScope{Customer}" />. Deliberately NOT a no-op scope — Admin's
///     original CustomerController gates nearly every action through a Sales Manager check
///     (<c>groupService.IsSalesManager(CurrentCustomer) &amp;&amp; CurrentCustomer.SeId !=
///     customer.SeId</c>, named <c>CheckSalesManager</c> in the original) that Store never had.
///     Same shape as ARCH-001 Order's <c>AdminOrderDataScope</c>. See Customer consolidation spec
///     "Scope classes".
/// </summary>
public class AdminCustomerDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<Customer>
{
    public async Task<bool> HasAccess(Customer entity)
    {
        if (entity is null) return false;
        if (entity.Deleted) return false;
        var isSalesManager = await groupService.IsSalesManager(contextAccessor.WorkContext.CurrentCustomer);
        return !isSalesManager || contextAccessor.WorkContext.CurrentCustomer.SeId == entity.SeId;
    }

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for Customer; required interface member
}
