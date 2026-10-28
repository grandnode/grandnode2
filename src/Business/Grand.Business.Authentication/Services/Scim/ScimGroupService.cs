using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     SCIM groups and their mapping to GrandNode roles (<see cref="IScimGroupService" />). Only SCIM-managed accounts are kept
///     as members; a membership change recomputes the roles of the members it adds or removes, a mapping change those of every
///     member. Members and mappings are written field by field (added and pulled one by one), so concurrent requests of the
///     provider and the administrator do not undo each other
/// </summary>
public class ScimGroupService(
    IRepository<ScimGroup> groups,
    IRepository<ScimUser> users,
    IGroupService groupService,
    IStoreService storeService,
    IVendorService vendorService,
    ScimRoleSync roleSync,
    IMediator mediator,
    ILogger<ScimGroupService> logger) : IScimGroupService
{
    public virtual async Task<ScimGroupResult> Create(ScimGroupData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var displayName = data.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName))
            return Invalid("displayName is required");
        if (await ExternalIdTaken(data.ExternalId, null))
            return Conflict();

        var group = new ScimGroup {
            DisplayName = displayName,
            ExternalId = data.ExternalId ?? "",
            Members = await ManagedOnly(data.MemberIds)
        };
        await groups.InsertAsync(group);
        await mediator.EntityInserted(group);
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
        if (await ExternalIdTaken(data.ExternalId, group.Id))
            return Conflict();

        await groups.UpdateField(group.Id, x => x.DisplayName, displayName);
        await groups.UpdateField(group.Id, x => x.ExternalId, data.ExternalId ?? "");
        return await ChangeMembers(group, await ManagedOnly(data.MemberIds), [], true);
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
        await mediator.EntityDeleted(group);
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
        return await ChangeMembers(group, await ManagedOnly(add), remove ?? [], replaceAll);
    }

    public virtual async Task<ScimGroupResult> Rename(string id, string displayName, string externalId)
    {
        var group = await Load(id);
        if (group == null)
            return new ScimGroupResult(ScimOutcome.NotFound);

        var name = displayName == null ? group.DisplayName : displayName.Trim();
        if (string.IsNullOrEmpty(name))
            return Invalid("displayName is required");
        if (externalId != null && await ExternalIdTaken(externalId, group.Id))
            return Conflict();

        await groups.UpdateField(group.Id, x => x.DisplayName, name);
        if (externalId != null)
            await groups.UpdateField(group.Id, x => x.ExternalId, externalId);
        var renamed = await groups.GetByIdAsync(group.Id);
        await mediator.EntityUpdated(renamed);
        return new ScimGroupResult(ScimOutcome.Ok, View(renamed));
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

        string target = "", store = "", vendor = "";
        if (!string.IsNullOrEmpty(customerGroupId))
        {
            var customerGroup = await groupService.GetCustomerGroupById(customerGroupId);
            if (!await ValidTarget(customerGroup, staffStoreId, vendorId))
                return ScimOutcome.Invalid;
            target = customerGroup.Id;
            store = IsSystemGroup(customerGroup, SystemCustomerGroupNames.StoreManager) ? staffStoreId : "";
            vendor = IsSystemGroup(customerGroup, SystemCustomerGroupNames.Vendors) ? vendorId : "";
        }

        await groups.UpdateField(group.Id, x => x.CustomerGroupId, target);
        await groups.UpdateField(group.Id, x => x.StaffStoreId, store);
        await groups.UpdateField(group.Id, x => x.VendorId, vendor);
        var mapped = await groups.GetByIdAsync(group.Id);
        await mediator.EntityUpdated(mapped);
        await roleSync.SyncMembers(mapped.Members);
        logger.LogInformation("SCIM group {GroupId} mapped to customer group {CustomerGroupId}", group.Id, target);
        return ScimOutcome.Ok;
    }

    /// <summary>
    ///     Adds and pulls members one by one (a replace writes the whole list) and recomputes the roles of the members whose
    ///     membership changed
    /// </summary>
    private async Task<ScimGroupResult> ChangeMembers(ScimGroup group, IList<string> add, IList<string> remove,
        bool replaceAll)
    {
        var before = group.Members.ToHashSet();
        var removed = remove.ToHashSet();
        var after = (replaceAll ? add : before.Concat(add)).Where(x => !removed.Contains(x)).ToHashSet();

        if (replaceAll)
            await groups.UpdateField(group.Id, x => x.Members, after.ToList());
        else
        {
            foreach (var id in after.Where(x => !before.Contains(x)))
                await groups.AddToCollectionField(group.Id, x => x.Members, id);
            foreach (var id in before.Where(x => !after.Contains(x)))
                await groups.RemoveCollectionFieldItem(group.Id, x => x.Members, m => m == id);
        }

        var changed = before.Except(after).Concat(after.Except(before)).ToList();
        var stored = await groups.GetByIdAsync(group.Id);
        await mediator.EntityUpdated(stored);
        await roleSync.SyncMembers(changed);
        return new ScimGroupResult(ScimOutcome.Ok, View(stored));
    }

    //Administrators and custom groups take no store or vendor; StoreManager needs an existing store, Vendors an existing vendor
    private async Task<bool> ValidTarget(CustomerGroup target, string staffStoreId, string vendorId)
    {
        if (!ScimRoleSync.IsMappable(target))
            return false;

        if (IsSystemGroup(target, SystemCustomerGroupNames.StoreManager))
            return string.IsNullOrEmpty(vendorId) && !string.IsNullOrEmpty(staffStoreId) &&
                   await storeService.GetStoreById(staffStoreId) != null;
        if (IsSystemGroup(target, SystemCustomerGroupNames.Vendors))
            return string.IsNullOrEmpty(staffStoreId) && !string.IsNullOrEmpty(vendorId) &&
                   await vendorService.GetVendorById(vendorId) != null;
        return string.IsNullOrEmpty(staffStoreId) && string.IsNullOrEmpty(vendorId);
    }

    private static bool IsSystemGroup(CustomerGroup group, string systemName) =>
        group.IsSystem && group.SystemName == systemName;

    //display names may repeat (Entra allows it); the provider's id of a group may not
    private async Task<bool> ExternalIdTaken(string externalId, string exceptId) =>
        !string.IsNullOrEmpty(externalId) &&
        await groups.AnyAsync(groups.Table.Where(x => x.Id != exceptId && x.ExternalId == externalId));

    //members that are not SCIM-managed accounts are dropped: a group can give roles only to accounts SCIM manages
    private async Task<List<string>> ManagedOnly(IEnumerable<string> ids)
    {
        var wanted = (ids ?? []).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
        if (wanted.Length == 0)
            return [];
        return (await users.ToListAsync(users.Table.Where(x => wanted.Contains(x.Id) && !x.Deprovisioned).Select(x => x.Id)))
            .ToList();
    }

    private async Task<ScimGroup> Load(string id) => string.IsNullOrEmpty(id) ? null : await groups.GetByIdAsync(id);

    private static ScimGroupView View(ScimGroup group) =>
        new(group.Id, group.DisplayName, string.IsNullOrEmpty(group.ExternalId) ? null : group.ExternalId,
            group.Members.ToList(), group.CreatedOnUtc, group.UpdatedOnUtc ?? group.CreatedOnUtc);

    private static ScimGroupResult Invalid(string detail) => new(ScimOutcome.Invalid, Detail: detail);

    private static ScimGroupResult Conflict() =>
        new(ScimOutcome.Conflict, Detail: "A group with this externalId already exists");
}
