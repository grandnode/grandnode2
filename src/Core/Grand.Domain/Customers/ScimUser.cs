namespace Grand.Domain.Customers;

/// <summary>
///     A staff account managed by SCIM provisioning: its <see cref="ParentEntity.Id" /> is the customer id, so one customer has
///     at most one record. It also remembers what SCIM granted, so SCIM takes away only its own grants
/// </summary>
public class ScimUser : BaseEntity
{
    /// <summary>The identity provider's id of the user (with Scim:ExternalIdIsSubject also its OpenID Connect subject)</summary>
    public string ExternalId { get; set; }

    /// <summary>Customer groups SCIM gave the account</summary>
    public List<string> GrantedGroupIds { get; set; } = [];

    /// <summary>Staff store SCIM set on the account; empty when SCIM set none</summary>
    public string GrantedStaffStoreId { get; set; }

    /// <summary>Vendor SCIM set on the account; empty when SCIM set none</summary>
    public string GrantedVendorId { get; set; }

    /// <summary>The account was deactivated by SCIM, so SCIM may activate it again (never an account disabled by hand)</summary>
    public bool DeactivatedByScim { get; set; }

    /// <summary>The mapped groups of the user give back-office roles that cannot be combined: SCIM granted none of them</summary>
    public bool RoleConflict { get; set; }
}
