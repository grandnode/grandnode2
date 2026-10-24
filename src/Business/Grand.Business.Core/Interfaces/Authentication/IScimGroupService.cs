using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     Groups pushed by the identity provider over SCIM and their mapping to GrandNode roles. Every change of a membership or
///     a mapping recomputes the roles of the members it touches
/// </summary>
public interface IScimGroupService
{
    Task<ScimGroupResult> Create(ScimGroupData data);

    Task<ScimGroupResult> Replace(string id, ScimGroupData data);

    Task<ScimGroupResult> Get(string id);

    /// <summary>The groups, optionally those with the display name or the externalId (exact, display name case-insensitive)</summary>
    Task<ScimPage<ScimGroupView>> List(string displayName, string externalId, int skip, int take);

    Task<ScimOutcome> Delete(string id);

    /// <summary>Adds and removes members; <paramref name="replaceAll" /> replaces the members with <paramref name="add" /></summary>
    Task<ScimGroupResult> ModifyMembers(string id, IList<string> add, IList<string> remove, bool replaceAll);

    /// <summary>Changes the name and the externalId only (null keeps the value)</summary>
    Task<ScimGroupResult> Rename(string id, string displayName, string externalId);

    Task<IList<ScimGroupMapping>> GetMappings();

    /// <summary>The customer groups a SCIM group can be mapped to</summary>
    Task<IList<CustomerGroup>> GetMappableGroups();

    /// <summary>
    ///     Maps the group to a customer group (Administrators; StoreManager with a store; Vendors with a vendor; a custom group);
    ///     an empty customer group removes the mapping. Invalid for any other target
    /// </summary>
    Task<ScimOutcome> SetMapping(string groupId, string customerGroupId, string staffStoreId, string vendorId);
}
