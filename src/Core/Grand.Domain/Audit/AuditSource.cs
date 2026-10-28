namespace Grand.Domain.Audit;

/// <summary>
///     Where the audited request came from
/// </summary>
public enum AuditSource
{
    Admin = 0,
    Store = 1,
    Vendor = 2,
    Api = 3,
    Scim = 4,
    Storefront = 5
}
