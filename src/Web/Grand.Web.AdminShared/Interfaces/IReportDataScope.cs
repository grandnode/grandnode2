#nullable enable

namespace Grand.Web.AdminShared.Interfaces;

/// <summary>
/// Per-host data-access strategy for the read-only Reports screens. Deliberately separate from
/// IAdminDataScope&lt;TEntity&gt;: Reports has no entity to load and access-check — every report is
/// an aggregation query parameterized by storeId/vendorId at the business-service layer (see
/// ARCH-001 Reports consolidation spec §2.3). Forcing Reports through IAdminDataScope&lt;TEntity&gt;
/// would require a fake TEntity and leave HasAccess/CanView permanently unused — a worse fit than a
/// second, smaller interface.
/// </summary>
public interface IReportDataScope
{
    /// <summary>Store id to force into report queries. "" (all stores) for Admin when the caller
    /// supplies no explicit store filter; the current staff store for Store; "" (not store-scoped)
    /// for Vendor.</summary>
    string StoreId { get; }

    /// <summary>Vendor id to force into report queries. "" for Admin/Store (unless the caller
    /// supplies an explicit vendor filter — Admin only); the current vendor's id for Vendor.</summary>
    string VendorId { get; }

    /// <summary>Whether the host's Bestsellers/report screens should render a store-picker field.
    /// True for Admin only (Store is implicitly scoped to its own store with no picker; Vendor has
    /// no store concept on these screens).</summary>
    bool ShowStoreSelector { get; }

    /// <summary>Whether the host's Bestsellers screen should render a vendor-picker field. True for
    /// Admin only.</summary>
    bool ShowVendorSelector { get; }

    /// <summary>Prefix used to build host-specific localization keys ("Admin" or "Vendor"). Store
    /// uses "Admin" — same precedent as every prior phase's ResourceKeyPrefix, Store has no distinct
    /// Reports resource set.</summary>
    string ResourceKeyPrefix { get; }

    /// <summary>Post-filter applied to bestsellers/product-bearing report rows after the underlying
    /// query returns, beyond the storeId/vendorId already passed into the query. Identity (no
    /// filtering) for Admin and Store. Vendor overrides this to additionally drop rows whose product
    /// the current vendor sub-account has no access to (WorkContext.HasAccessToProduct) — preserves
    /// Vendor's existing BestsellersReportList behavior exactly (see spec §2.3).</summary>
    bool CanIncludeProduct(Grand.Domain.Catalog.Product product) => true;
}
