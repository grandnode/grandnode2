#nullable enable

namespace Grand.Web.AdminShared.Interfaces;

/// <summary>
///     Per-host data-access strategy for an admin-area entity. Implemented once per host
///     (Admin/Store/Vendor) and injected into shared AdminShared controllers/services so
///     scope logic lives in one place instead of being duplicated per host.
/// </summary>
public interface IAdminDataScope<TEntity>
{
    /// <summary>Whether the current user may mutate (edit/delete) this specific, already-loaded entity.
    /// This is the strict check — for Store, matches AclMappingExtension.AccessToEntityByStore exactly
    /// (denies global and multi-store entities, only the entity's exclusive single store passes).</summary>
    Task<bool> HasAccess(TEntity entity);

    /// <summary>Whether the current user may view/reference this entity (open its edit form, copy it) —
    /// looser than <see cref="HasAccess"/> for hosts where viewing a shared/global entity is allowed but
    /// mutating it isn't. Defaults to <see cref="HasAccess"/> for hosts with no such split (Admin: always
    /// true either way; Vendor: the two are identical, verified against the existing, unsplit
    /// `CheckAccessToProduct`). Only Store overrides this.</summary>
    Task<bool> CanView(TEntity entity) => HasAccess(entity);

    /// <summary>Narrows a query to the entities the current user may see. No-op for global (Admin) scope.</summary>
    IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query);

    /// <summary>Store id to default onto new/edited entities. Null when the host has no store concept
    /// (Admin: global, no default; Vendor: not store-scoped at all).</summary>
    string? DefaultStoreId { get; }

    /// <summary>Prefix used to build host-specific localization keys, e.g. "Admin", "Vendor". Store
    /// currently has no distinct resource set and uses "Admin" (see Task 6).</summary>
    string ResourceKeyPrefix { get; }
}
