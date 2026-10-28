using Grand.Domain.Customers;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>What the mapped SCIM groups of one user give</summary>
/// <param name="Conflict">The back-office roles cannot be combined, so none of them is given</param>
public record ScimWanted(IReadOnlyCollection<string> GroupIds, string StaffStoreId, string VendorId, bool Conflict);

/// <summary>
///     The roles a user gets from its mapped SCIM groups. The back-office roles must form a combination the customer validator
///     accepts - Administrators alone, StoreManager with one store, Vendors with one vendor - otherwise none of them is given
///     (least privilege: guessing would open the wrong store or panel); custom customer groups are given in every case
/// </summary>
public static class ScimRoles
{
    /// <param name="groups">The SCIM groups the user belongs to</param>
    /// <param name="systemNameOf">System name of a customer group; empty for a custom group</param>
    public static ScimWanted Wanted(IEnumerable<ScimGroup> groups, Func<string, string> systemNameOf)
    {
        var mapped = groups.Where(x => !string.IsNullOrEmpty(x.CustomerGroupId)).ToList();
        var custom = new HashSet<string>();
        var admins = new HashSet<string>();
        var storeManagers = new HashSet<string>();
        var vendorGroups = new HashSet<string>();
        var stores = new HashSet<string>();
        var vendors = new HashSet<string>();

        foreach (var group in mapped)
            switch (systemNameOf(group.CustomerGroupId))
            {
                case SystemCustomerGroupNames.Administrators:
                    admins.Add(group.CustomerGroupId);
                    break;
                case SystemCustomerGroupNames.StoreManager:
                    storeManagers.Add(group.CustomerGroupId);
                    if (!string.IsNullOrEmpty(group.StaffStoreId))
                        stores.Add(group.StaffStoreId);
                    break;
                case SystemCustomerGroupNames.Vendors:
                    vendorGroups.Add(group.CustomerGroupId);
                    if (!string.IsNullOrEmpty(group.VendorId))
                        vendors.Add(group.VendorId);
                    break;
                default:
                    custom.Add(group.CustomerGroupId);
                    break;
            }

        var kinds = (admins.Count > 0 ? 1 : 0) + (storeManagers.Count > 0 ? 1 : 0) + (vendorGroups.Count > 0 ? 1 : 0);
        var conflict = kinds > 1 ||
                       (storeManagers.Count > 0 && stores.Count != 1) ||
                       (vendorGroups.Count > 0 && vendors.Count != 1);
        if (conflict)
            return new ScimWanted(custom, null, null, true);

        var wanted = new HashSet<string>(custom);
        wanted.UnionWith(admins);
        wanted.UnionWith(storeManagers);
        wanted.UnionWith(vendorGroups);
        return new ScimWanted(wanted, stores.SingleOrDefault(), vendors.SingleOrDefault(), false);
    }
}
