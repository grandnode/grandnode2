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
    /// true either way for most entities; Vendor: the two are identical, verified against the existing,
    /// unsplit `CheckAccessToProduct`). Overridden by Store, and by
    /// <see cref="Grand.Web.AdminShared.Services.AdminDiscountDataScope"/> for its Store-Manager-gated
    /// Admin case.</summary>
    Task<bool> CanView(TEntity entity) => HasAccess(entity);

    /// <summary>Store id to default onto new/edited entities. Null when the host has no store concept
    /// (Admin: global, no default; Vendor: not store-scoped at all).</summary>
    string? DefaultStoreId { get; }

    /// <summary>Async counterpart to <see cref="DefaultStoreId"/>. Every controller/service call site
    /// in this project is already async, so callers should always prefer this member. It exists
    /// because <see cref="Grand.Web.AdminShared.Services.AdminDiscountDataScope"/> computes its
    /// default store id from an async customer-group check (<c>IGroupService.IsStoreManager</c>) with
    /// no sync-safe path — exposing only the synchronous <see cref="DefaultStoreId"/> would force it
    /// to block on that Task (`.GetAwaiter().GetResult()`), which is a hard constraint violation
    /// (see `.ai/constraints.md` "Never block on a Task") and a thread-pool starvation risk under
    /// load. Every other scope's default store id needs no I/O, so the default implementation here
    /// just wraps <see cref="DefaultStoreId"/> and costs those implementations nothing.</summary>
    Task<string?> GetDefaultStoreIdAsync() => Task.FromResult(DefaultStoreId);

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

    /// <summary>Order line items visible to the current host — e.g. Vendor sees only its own
    /// items within a mixed-vendor order. Identity (no filtering) for hosts with no such
    /// restriction (Admin, Store, and every non-Order entity). Only
    /// <see cref="Grand.Web.AdminShared.Services.VendorOrderDataScope"/> and
    /// <see cref="Grand.Web.AdminShared.Services.VendorShipmentDataScope"/> override this. Lives on
    /// the shared interface rather than a separate Order-only interface because
    /// IAdminDataScope&lt;TEntity&gt; is already the single per-host strategy object injected
    /// into BaseOrderController/OrderViewModelService — see ARCH-001 Order consolidation spec
    /// §3.4.</summary>
    IEnumerable<Grand.Domain.Orders.OrderItem> FilterOrderItems(
        IEnumerable<Grand.Domain.Orders.OrderItem> orderItems) => orderItems;
}
