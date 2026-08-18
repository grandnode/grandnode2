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

    /// <summary>Whether the host's product list/search screens should offer a store picker at all.
    /// True for Admin and Store (both operate within a store concept); false for Vendor (vendors don't
    /// pick stores - the whole point of vendor scope is that it isn't a store id). This is a capability
    /// flag, deliberately distinct from <see cref="DefaultStoreId"/> being null: DefaultStoreId is also
    /// null for Admin (global, no default store), where the selector should still show.</summary>
    bool ShowStoreSelector { get; }

    /// <summary>Vendor id to force onto product search/listing queries, overriding whatever a caller-
    /// supplied search model asks for. Null when the host has no vendor concept (Admin: global; Store:
    /// store-scoped, not vendor-scoped). Vendor: the current vendor's id - mirrors Vendor's original
    /// service always passing <c>vendorId: CurrentVendor.Id</c> into <c>IProductService.SearchProducts</c>/
    /// <c>PrepareProductList</c> regardless of any vendor filter a client-supplied model field might carry,
    /// so a vendor can never search or bulk-list another vendor's products.</summary>
    string? DefaultVendorId { get; }

    /// <summary>Whether the host's product list/search screens should offer the "Show on homepage"
    /// filter option. True for Global and Store; false for Vendor - vendors can't feature products
    /// on the homepage, a real capability difference, not a naming difference. Replaces the earlier
    /// `ResourceKeyPrefix != "Vendor"` check in ProductViewModelService, which overloaded a
    /// localization-key property for behavior gating.</summary>
    bool CanFeatureOnHomepage { get; }
}
