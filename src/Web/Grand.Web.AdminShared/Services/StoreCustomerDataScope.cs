#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;
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
