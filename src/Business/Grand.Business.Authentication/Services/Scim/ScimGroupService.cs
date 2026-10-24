using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     SCIM groups and their mapping to GrandNode roles (<see cref="IScimGroupService" />). Only SCIM-managed accounts are kept
///     as members; every change recomputes the roles of the members it adds or removes
/// </summary>
public class ScimGroupService(
    IRepository<ScimGroup> groups,
    IRepository<ScimUser> users,
    IGroupService groupService,
    IStoreService storeService,
    IVendorService vendorService,
    ScimRoleSync roleSync,
    ILogger<ScimGroupService> logger) : IScimGroupService
{
    public virtual async Task<ScimGroupResult> Create(ScimGroupData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var displayName = data.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName))
            return Invalid("displayName is required");
        var refusal = await UniquenessRefusal(displayName, data.ExternalId, null);
        if (refusal != null)
            return new ScimGroupResult(ScimOutcome.Conflict, Detail: refusal);

        var group = new ScimGroup {
            DisplayName = displayName,
            ExternalId = data.ExternalId ?? "",
            Members = await ManagedOnly(data.MemberIds)
        };
        await groups.InsertAsync(group);
        await roleSync.SyncMembers(group.Members);
        logger.LogInformation("SCIM group {GroupId} created with {Count} members", group.Id, group.Members.Count);
        return new ScimGroupResult(ScimOutcome.Created, View(group));
    }

    public virtual async Task<ScimGroupResult> Replace(string id, ScimGroupData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var group = await Load(id);
        if (group == null)
            return new ScimGroupResult(ScimOutcome.NotFound);

        var displayName = data.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName))
            return Invalid("displayName is required");
        var refusal = await UniquenessRefusal(displayName, data.ExternalId, group.Id);
        if (refusal != null)
            return new ScimGroupResult(ScimOutcome.Conflict, Detail: refusal);

        var before = group.Members.ToList();
        group.DisplayName = displayName;
        group.ExternalId = data.ExternalId ?? "";
        group.Members = await ManagedOnly(data.MemberIds);
        await groups.UpdateAsync(group);
        await roleSync.SyncMembers(before.Concat(group.Members));
        return new ScimGroupResult(ScimOutcome.Ok, View(group));
    }

    public virtual async Task<ScimGroupResult> Get(string id)
    {
        var group = await Load(id);
        return group == null ? new ScimGroupResult(ScimOutcome.NotFound) : new ScimGroupResult(ScimOutcome.Ok, View(group));
    }

    public virtual async Task<ScimPage<ScimGroupView>> List(string displayName, string externalId, int skip, int take)
    {
        var query = groups.Table;
        if (!string.IsNullOrEmpty(externalId))
            query = query.Where(x => x.ExternalId == externalId);
        if (!string.IsNullOrEmpty(displayName))
        {
            var lowered = displayName.Trim().ToLowerInvariant();
            query = query.Where(x => x.DisplayName.ToLower() == lowered);
        }

        var total = await groups.CountAsync(query);
        var items = take <= 0
            ? []
            : await groups.ToListAsync(query.OrderBy(x => x.CreatedOnUtc).ThenBy(x => x.Id).Skip(Math.Max(skip, 0)).Take(take));
        return new ScimPage<ScimGroupView>(items.Select(View).ToList(), total);
    }

    public virtual async Task<ScimOutcome> Delete(string id)
    {
        var group = await Load(id);
        if (group == null)
            return ScimOutcome.NotFound;

        await groups.DeleteAsync(group);
        await roleSync.SyncMembers(group.Members);
        logger.LogInformation("SCIM group {GroupId} deleted", group.Id);
        return ScimOutcome.Ok;
    }

    public virtual async Task<ScimGroupResult> ModifyMembers(string id, IList<string> add, IList<string> remove,
        bool replaceAll)
    {
        var group = await Load(id);
        if (group == null)
            return new ScimGroupResult(ScimOutcome.NotFound);

        var before = group.Members.ToList();
        var added = await ManagedOnly(add);
        var removed = (remove ?? []).ToHashSet();
        group.Members = (replaceAll ? added : before.Concat(added))
            .Where(x => !removed.Contains(x))
            .Distinct()
            .ToList();
        await groups.UpdateAsync(group);
        await roleSync.SyncMembers(before.Concat(group.Members));
        return new ScimGroupResult(ScimOutcome.Ok, View(group));
    }

    public virtual async Task<ScimGroupResult> Rename(string id, string displayName, string externalId)
    {
        var group = await Load(id);
        if (group == null)
            return new ScimGroupResult(ScimOutcome.NotFound);

        var name = displayName == null ? group.DisplayName : displayName.Trim();
        if (string.IsNullOrEmpty(name))
            return Invalid("displayName is required");
        var refusal = await UniquenessRefusal(name, externalId, group.Id);
        if (refusal != null)
            return new ScimGroupResult(ScimOutcome.Conflict, Detail: refusal);

        group.DisplayName = name;
        if (externalId != null)
            group.ExternalId = externalId;
        await groups.UpdateAsync(group);
        return new ScimGroupResult(ScimOutcome.Ok, View(group));
    }

    public virtual async Task<IList<ScimGroupMapping>> GetMappings() =>
        (await groups.ToListAsync(groups.Table.OrderBy(x => x.DisplayName)))
        .Select(x => new ScimGroupMapping(x.Id, x.DisplayName, x.Members.Count, x.CustomerGroupId, x.StaffStoreId,
            x.VendorId))
        .ToList();

    public virtual async Task<IList<CustomerGroup>> GetMappableGroups() =>
        (await groupService.GetAllCustomerGroups(showHidden: true)).Where(ScimRoleSync.IsMappable).ToList();

    public virtual async Task<ScimOutcome> SetMapping(string groupId, string customerGroupId, string staffStoreId,
        string vendorId)
    {
        var group = await Load(groupId);
        if (group == null)
            return ScimOutcome.NotFound;

        if (string.IsNullOrEmpty(customerGroupId))
        {
            group.CustomerGroupId = "";
            group.StaffStoreId = "";
            group.VendorId = "";
        }
        else
        {
            var target = await groupService.GetCustomerGroupById(customerGroupId);
            if (!await ValidTarget(target, staffStoreId, vendorId))
                return ScimOutcome.Invalid;
            group.CustomerGroupId = target.Id;
            group.StaffStoreId = target.SystemName == SystemCustomerGroupNames.StoreManager && target.IsSystem
                ? staffStoreId
                : "";
            group.VendorId = target.SystemName == SystemCustomerGroupNames.Vendors && target.IsSystem ? vendorId : "";
        }

        await groups.UpdateAsync(group);
        await roleSync.SyncMembers(group.Members);
        logger.LogInformation("SCIM group {GroupId} mapped to customer group {CustomerGroupId}", group.Id,
            group.CustomerGroupId);
        return ScimOutcome.Ok;
    }

    //Administrators and custom groups take no store or vendor; StoreManager needs an existing store, Vendors an existing vendor
    private async Task<bool> ValidTarget(CustomerGroup target, string staffStoreId, string vendorId)
    {
        if (!ScimRoleSync.IsMappable(target))
            return false;

        var isStoreManager = target.IsSystem && target.SystemName == SystemCustomerGroupNames.StoreManager;
        var isVendors = target.IsSystem && target.SystemName == SystemCustomerGroupNames.Vendors;
        if (isStoreManager)
            return string.IsNullOrEmpty(vendorId) && !string.IsNullOrEmpty(staffStoreId) &&
                   await storeService.GetStoreById(staffStoreId) != null;
        if (isVendors)
            return string.IsNullOrEmpty(staffStoreId) && !string.IsNullOrEmpty(vendorId) &&
                   await vendorService.GetVendorById(vendorId) != null;
        return string.IsNullOrEmpty(staffStoreId) && string.IsNullOrEmpty(vendorId);
    }

    private async Task<string> UniquenessRefusal(string displayName, string externalId, string exceptId)
    {
        var lowered = displayName.ToLowerInvariant();
        if (await groups.AnyAsync(groups.Table.Where(x => x.Id != exceptId && x.DisplayName.ToLower() == lowered)))
            return "A group with this displayName already exists";
        if (!string.IsNullOrEmpty(externalId) &&
            await groups.AnyAsync(groups.Table.Where(x => x.Id != exceptId && x.ExternalId == externalId)))
            return "A group with this externalId already exists";
        return null;
    }

    //members that are not SCIM-managed accounts are dropped: a group can give roles only to accounts SCIM manages
    private async Task<List<string>> ManagedOnly(IEnumerable<string> ids)
    {
        var wanted = (ids ?? []).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
        if (wanted.Length == 0)
            return [];
        return (await users.ToListAsync(users.Table.Where(x => wanted.Contains(x.Id)).Select(x => x.Id))).ToList();
    }

    private async Task<ScimGroup> Load(string id) => string.IsNullOrEmpty(id) ? null : await groups.GetByIdAsync(id);

    private static ScimGroupView View(ScimGroup group) =>
        new(group.Id, group.DisplayName, string.IsNullOrEmpty(group.ExternalId) ? null : group.ExternalId,
            group.Members.ToList(), group.CreatedOnUtc, group.UpdatedOnUtc ?? group.CreatedOnUtc);

    private static ScimGroupResult Invalid(string detail) => new(ScimOutcome.Invalid, Detail: detail);
}
