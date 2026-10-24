using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     Writes the roles a SCIM-managed account gets from its mapped groups (<see cref="ScimRoles" />): customer groups, staff
///     store and vendor. It takes away only what SCIM granted (<see cref="ScimUser" />), so groups an administrator assigned by
///     hand stay. System, deleted and break-glass accounts are never touched. Only the fields of the record it owns are
///     written, so a concurrent change of another field (a deactivation) is not overwritten
/// </summary>
public class ScimRoleSync(
    IRepository<ScimUser> users,
    IRepository<ScimGroup> groups,
    ICustomerService customerService,
    IGroupService groupService,
    OidcLoginConfig loginConfig,
    IMediator mediator,
    ILogger<ScimRoleSync> logger)
{
    private static readonly string[] BackOfficeGroups = [
        SystemCustomerGroupNames.Administrators, SystemCustomerGroupNames.StoreManager, SystemCustomerGroupNames.Vendors
    ];

    /// <summary>A customer group SCIM may give: Administrators, StoreManager, Vendors or a custom group</summary>
    public static bool IsMappable(CustomerGroup group) =>
        group != null && (!group.IsSystem || BackOfficeGroups.Contains(group.SystemName));

    /// <summary>Recomputes the roles of the account from the groups it belongs to and stores what SCIM granted</summary>
    public virtual async Task Sync(Customer customer, ScimUser record)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(record);

        //an account may have become a break-glass account since it was provisioned
        if (ScimProtection.Refusal(customer, loginConfig) != null)
        {
            logger.LogWarning("SCIM user {CustomerId} is a protected account; its roles are left as they are", customer.Id);
            return;
        }

        var customerId = customer.Id;
        var memberOf = await groups.ToListAsync(groups.Table.Where(x => x.Members.Contains(customerId)));
        var targetIds = memberOf.Select(x => x.CustomerGroupId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
        var targets = targetIds.Length == 0
            ? new Dictionary<string, CustomerGroup>()
            : (await groupService.GetAllByIds(targetIds)).Where(IsMappable).ToDictionary(x => x.Id);

        //a mapping to a group that was deleted or became unmappable gives nothing
        var wanted = ScimRoles.Wanted(memberOf.Where(x => targets.ContainsKey(x.CustomerGroupId ?? "")),
            id => targets[id].IsSystem ? targets[id].SystemName : "");

        if (wanted.Conflict && !record.RoleConflict)
            logger.LogWarning("SCIM user {CustomerId}: the mapped groups give back-office roles that cannot be combined; " +
                              "none of them is granted", customerId);
        await Apply(customer, record, wanted);
    }

    /// <summary>Takes away every role SCIM granted</summary>
    public virtual Task Revoke(Customer customer, ScimUser record) =>
        Apply(customer, record, new ScimWanted([], null, null, false));

    /// <summary>Recomputes the roles of the SCIM-managed accounts among the customers</summary>
    public virtual async Task SyncMembers(IEnumerable<string> customerIds)
    {
        foreach (var id in customerIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
        {
            var record = await users.GetByIdAsync(id);
            if (record is not { Deprovisioned: false })
                continue;
            var customer = await customerService.GetCustomerById(id);
            if (customer != null)
                await Sync(customer, record);
        }
    }

    /// <summary>
    ///     An account SCIM takes over is managed by SCIM from then on: its back-office roles (with its staff store and vendor)
    ///     and the mapped groups it holds count as granted by SCIM, so the next sync keeps only what the identity provider gives
    /// </summary>
    public virtual async Task TakeOver(Customer customer, ScimUser record)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(record);

        var managed = new HashSet<string>(await groups.ToListAsync(groups.Table
            .Where(x => x.CustomerGroupId != null && x.CustomerGroupId != "")
            .Select(x => x.CustomerGroupId)));
        foreach (var systemName in BackOfficeGroups)
        {
            var group = await groupService.GetCustomerGroupBySystemName(systemName);
            if (group != null)
                managed.Add(group.Id);
        }

        record.GrantedGroupIds = customer.Groups.Where(managed.Contains).ToList();
        record.GrantedStaffStoreId = customer.StaffStoreId;
        record.GrantedVendorId = customer.VendorId;
    }

    private async Task Apply(Customer customer, ScimUser record, ScimWanted wanted)
    {
        var current = customer.Groups.ToHashSet();
        var granted = record.GrantedGroupIds ?? [];
        var toRemove = granted.Where(x => !wanted.GroupIds.Contains(x) && current.Contains(x)).ToList();
        var toAdd = wanted.GroupIds.Where(x => !current.Contains(x)).ToList();

        var changed = toRemove.Concat(toAdd).Distinct().ToArray();
        var known = changed.Length == 0
            ? new Dictionary<string, CustomerGroup>()
            : (await groupService.GetAllByIds(changed)).ToDictionary(x => x.Id);
        //a granted group deleted since is still taken away by its id
        foreach (var id in toRemove)
            await customerService.DeleteCustomerGroupInCustomer(known.GetValueOrDefault(id) ?? new CustomerGroup { Id = id },
                customer.Id);
        foreach (var id in toAdd.Where(known.ContainsKey))
            await customerService.InsertCustomerGroupInCustomer(known[id], customer.Id);

        var grantedGroups = wanted.GroupIds.Where(x => current.Contains(x) || known.ContainsKey(x)).ToList();
        var grantedStore = await ApplyField(customer.Id, customer.StaffStoreId, record.GrantedStaffStoreId,
            wanted.StaffStoreId, x => x.StaffStoreId);
        var grantedVendor = await ApplyField(customer.Id, customer.VendorId, record.GrantedVendorId, wanted.VendorId,
            x => x.VendorId);

        //each field alone: a whole-record write would undo a concurrent change of another field
        var recordChanged = false;
        if (!grantedGroups.ToHashSet().SetEquals(granted))
        {
            await users.UpdateField(record.Id, x => x.GrantedGroupIds, grantedGroups);
            record.GrantedGroupIds = grantedGroups;
            recordChanged = true;
        }

        if (grantedStore != record.GrantedStaffStoreId)
        {
            await users.UpdateField(record.Id, x => x.GrantedStaffStoreId, grantedStore);
            record.GrantedStaffStoreId = grantedStore;
            recordChanged = true;
        }

        if (grantedVendor != record.GrantedVendorId)
        {
            await users.UpdateField(record.Id, x => x.GrantedVendorId, grantedVendor);
            record.GrantedVendorId = grantedVendor;
            recordChanged = true;
        }

        if (wanted.Conflict != record.RoleConflict)
        {
            await users.UpdateField(record.Id, x => x.RoleConflict, wanted.Conflict);
            record.RoleConflict = wanted.Conflict;
            recordChanged = true;
        }

        if (recordChanged)
            await mediator.EntityUpdated(record);

        if (toRemove.Count > 0 || toAdd.Count > 0)
            logger.LogInformation("SCIM user {CustomerId}: {Added} customer groups granted, {Removed} taken away", customer.Id,
                toAdd.Count, toRemove.Count);
    }

    //sets the wanted value, or clears the value SCIM set when nothing is wanted; returns what SCIM granted now
    private async Task<string> ApplyField(string customerId, string current, string granted, string wanted,
        Expression<Func<Customer, string>> field)
    {
        if (!string.IsNullOrEmpty(wanted))
        {
            if (current != wanted)
                await customerService.UpdateCustomerField(customerId, field, wanted);
            return wanted;
        }

        if (!string.IsNullOrEmpty(granted) && current == granted)
            await customerService.UpdateCustomerField(customerId, field, "");
        return null;
    }
}
