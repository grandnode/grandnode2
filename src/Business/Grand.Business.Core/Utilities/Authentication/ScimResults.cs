namespace Grand.Business.Core.Utilities.Authentication;

/// <summary>Result of a SCIM provisioning operation; the endpoint maps it to the HTTP status</summary>
public enum ScimOutcome
{
    Ok,
    Created,
    NotFound,

    /// <summary>The account or group cannot be managed this way (uniqueness, a protected account)</summary>
    Conflict,

    /// <summary>The request lacks or breaks a required value</summary>
    Invalid
}

/// <summary>The attributes of a SCIM user that GrandNode keeps</summary>
public record ScimUserData(string UserName, string ExternalId, string Email, string GivenName, string FamilyName, bool Active);

public record ScimGroupRef(string Id, string DisplayName);

/// <summary>A SCIM user as the endpoint returns it (id = customer id)</summary>
public record ScimUserView(
    string Id,
    string ExternalId,
    string UserName,
    string Email,
    string GivenName,
    string FamilyName,
    bool Active,
    IList<ScimGroupRef> Groups,
    DateTime Created,
    DateTime? LastModified);

/// <param name="Detail">Why the operation was refused, for the SCIM error; never an e-mail or another personal value</param>
public record ScimUserResult(ScimOutcome Outcome, ScimUserView User = null, string Detail = null);

/// <summary>The attributes of a SCIM group that GrandNode keeps; members are SCIM user ids (customer ids)</summary>
public record ScimGroupData(string DisplayName, string ExternalId, IList<string> MemberIds);

public record ScimGroupView(
    string Id,
    string DisplayName,
    string ExternalId,
    IList<string> MemberIds,
    DateTime Created,
    DateTime? LastModified);

/// <param name="Detail">Why the operation was refused, for the SCIM error</param>
public record ScimGroupResult(ScimOutcome Outcome, ScimGroupView Group = null, string Detail = null);

public record ScimPage<T>(IList<T> Items, int Total);

/// <summary>A group received from the identity provider and the role an administrator mapped to it</summary>
public record ScimGroupMapping(
    string GroupId,
    string DisplayName,
    int MemberCount,
    string CustomerGroupId,
    string StaffStoreId,
    string VendorId);
