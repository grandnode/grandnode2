#nullable enable

namespace Grand.Web.AdminShared.Interfaces;

/// <summary>
///     Per-host data-access strategy for an admin-area entity. Implemented once per host
///     (Admin/Store/Vendor) and injected into shared AdminShared controllers/services so
///     scope logic lives in one place instead of being duplicated per host.
/// </summary>
public interface IAdminDataScope<TEntity>
{
    /// <summary>Whether the current user may access this specific, already-loaded entity.</summary>
    Task<bool> HasAccess(TEntity entity);

    /// <summary>Narrows a query to the entities the current user may see. No-op for global (Admin) scope.</summary>
    IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query);

    /// <summary>Store id to default onto new/edited entities. Null when the host has no store concept
    /// (Admin: global, no default; Vendor: not store-scoped at all).</summary>
    string? DefaultStoreId { get; }

    /// <summary>Prefix used to build host-specific localization keys, e.g. "Admin", "Vendor". Store
    /// currently has no distinct resource set and uses "Admin" (see Task 6).</summary>
    string ResourceKeyPrefix { get; }
}
