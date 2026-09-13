#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IAdminDataScope{Order}" />. Deliberately NOT the generic
///     <see cref="GlobalAdminDataScope{TEntity}" /> — Admin's original OrderController gates
///     nearly every action through a Sales Manager check
///     (<c>groupService.IsSalesManager(CurrentCustomer) &amp;&amp; CurrentCustomer.SeId !=
///     order.SeId</c>) that Store and Vendor never had. Reusing the always-true generic scope here
///     would silently drop that restriction. See ARCH-001 Order consolidation spec §3.2.
/// </summary>
public class AdminOrderDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<Order>
{
    public async Task<bool> HasAccess(Order entity)
    {
        if (entity is null) return false;
        var isSalesManager = await groupService.IsSalesManager(contextAccessor.WorkContext.CurrentCustomer);
        return !isSalesManager || contextAccessor.WorkContext.CurrentCustomer.SeId == entity.SeId;
    }

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for Order; required interface member
}
