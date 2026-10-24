#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;
/// <summary>
///     The Store panel manages its store's registered customers and nothing more: never the staff - administrators,
///     vendors, store managers, sales managers, or any account tied to a vendor or a staff store - whose accounts
///     a store manager could otherwise reset and take over.
/// </summary>
public class StoreCustomerDataScope(IContextAccessor contextAccessor, IGroupService groupService)
    : IAdminDataScope<Customer>
{
    /// <summary>Groups whose members are staff, left out of the Store panel's customer list</summary>
    public static readonly string[] StaffGroupSystemNames = [
        SystemCustomerGroupNames.Administrators, SystemCustomerGroupNames.Vendors,
        SystemCustomerGroupNames.StoreManager, SystemCustomerGroupNames.SalesManager
    ];

    public async Task<bool> HasAccess(Customer entity)
    {
        if (entity is null || entity.Deleted) return false;
        if (entity.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return false;
        if (!string.IsNullOrEmpty(entity.VendorId) || !string.IsNullOrEmpty(entity.StaffStoreId)) return false;
        if (await groupService.IsAdmin(entity) || await groupService.IsVendor(entity) ||
            await groupService.IsStoreManager(entity) || await groupService.IsSalesManager(entity))
            return false;
        return await groupService.IsRegistered(entity);
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true;
}
