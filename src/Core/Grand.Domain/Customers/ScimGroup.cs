namespace Grand.Domain.Customers;

/// <summary>
///     A group pushed by the identity provider over SCIM, and the GrandNode role its members get. A group grants nothing until an
///     administrator maps it
/// </summary>
public class ScimGroup : BaseEntity
{
    public string DisplayName { get; set; }

    /// <summary>The identity provider's id of the group</summary>
    public string ExternalId { get; set; }

    /// <summary>Ids of the SCIM-managed customers in the group</summary>
    public List<string> Members { get; set; } = [];

    /// <summary>Customer group the members get; empty while the group is not mapped</summary>
    public string CustomerGroupId { get; set; }

    /// <summary>Staff store of the members, with the StoreManager group</summary>
    public string StaffStoreId { get; set; }

    /// <summary>Vendor of the members, with the Vendors group</summary>
    public string VendorId { get; set; }
}
