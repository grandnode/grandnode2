using Grand.Business.Core.Utilities.Authentication;

namespace Grand.Business.Core.Interfaces.Authentication;

/// <summary>
///     SCIM provisioning of staff accounts: the identity provider creates (or takes over by e-mail), updates, deactivates and
///     deletes the GrandNode account of an employee. A deleted SCIM user keeps its customer, inactive and without the roles SCIM
///     granted. System, deleted and break-glass accounts are never managed
/// </summary>
public interface IScimUserService
{
    Task<ScimUserResult> Create(ScimUserData data);

    /// <param name="id">SCIM user id (the customer id)</param>
    Task<ScimUserResult> Replace(string id, ScimUserData data);

    Task<ScimUserResult> Get(string id);

    /// <summary>The managed users, optionally those with the user name or the externalId (exact, user name case-insensitive)</summary>
    Task<ScimPage<ScimUserView>> List(string userName, string externalId, int skip, int take);

    Task<ScimOutcome> Delete(string id);

    /// <summary>Customer ids of the managed users whose mapped groups give back-office roles that cannot be combined</summary>
    Task<IList<string>> GetConflicts();

    /// <summary>How many accounts SCIM manages</summary>
    Task<int> Count();
}
