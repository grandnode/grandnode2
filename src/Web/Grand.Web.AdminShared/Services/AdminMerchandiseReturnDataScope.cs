#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IAdminDataScope{MerchandiseReturn}" />: a sales manager reaches the returns of their own
///     orders only, as <see cref="AdminOrderDataScope" /> narrows the orders. A return carries the sales employee of
///     its order, copied when the return was made.
/// </summary>
public class AdminMerchandiseReturnDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<MerchandiseReturn>
{
    public async Task<bool> HasAccess(MerchandiseReturn entity)
    {
        if (entity is null) return false;
        var isSalesManager = await groupService.IsSalesManager(contextAccessor.WorkContext.CurrentCustomer);
        return !isSalesManager || contextAccessor.WorkContext.CurrentCustomer.SeId == entity.SeId;
    }

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for MerchandiseReturn; required interface member
}