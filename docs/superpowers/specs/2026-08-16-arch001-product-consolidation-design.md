# ARCH-001 — Product panel consolidation design

Date: 2026-08-16
Status: Approved, ready for implementation planning

## Problem

`Grand.Web.Admin`, `Grand.Web.Store`, and `Grand.Web.Vendor` each carry their own
copy of `ProductController` (2478 / 2625 / 2584 lines) and, for the view-model
layer, `Grand.Web.AdminShared` and `Grand.Web.Vendor` each carry their own
`ProductViewModelService` (2571 / 2381 lines, 1768 lines of diff). The copies
have drifted too far to merge mechanically. A bug fix or security patch in the
product editor currently requires three independent edits, and history shows
that requirement gets missed (commits #754, #765 fixed antiforgery handling in
some panels but not all). Full finding recorded in project memory
`project_arch001_triple_admin_duplication.md`.

This spec covers **only the `Product` vertical** (`ProductController` +
`ProductViewModelService` + `Product` views) as the first, highest-value slice
of ARCH-001. It does not attempt to generalize to every entity yet, though the
core abstraction is named and shaped so Order/Category/Collection can adopt it
later without redesign.

## Existing precedent

`Grand.Web.AdminShared/Controllers/BaseLoginController.cs` already implements
this exact pattern: an abstract base controller in AdminShared, with three
21-line subclasses (`Grand.Web.Admin/Store/Vendor/Controllers/LoginController.cs`)
that add only `[Area(...)]` and pass constructor args through. This design
follows that precedent at Product's scale.

Recent groundwork already in place (as of 2026-08-16):
- Characterization tests exist for all three `ProductController`s
  (`src/Tests/Grand.Web.{Admin,Store,Vendor}.Tests/Controllers/ProductControllerTests.cs`)
  and for both `ProductViewModelService`s (`Grand.Web.Admin.Tests` covers
  AdminShared's, `Grand.Web.Vendor.Tests` covers Vendor's).
- #786 deduped Store's `ProductController` access checks onto a single
  `CanAccessProduct` helper.
- #785 deduped Vendor's access checks similarly.
- #788 synced Vendor's `ProductViewModelService` to AdminShared's
  primary-constructor style, reducing incidental diff noise before a merge.

These give a safety net for a direct migration (no parallel-run / feature flag
needed — chosen deliberately over a flagged rollout given the test coverage
already in place).

## Current access-scope patterns (what `IAdminDataScope` must replace)

- **Admin**: no filtering — global access to all products.
- **Store** (`Grand.Web.Store/Controllers/ProductController.cs`): scattered
  direct reads of `_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId`,
  used both to filter lists/queries and as a default value written onto new/
  edited products (`model.StoreId`, `model.Stores`, list search filters).
- **Vendor** (`Grand.Web.Vendor/Controllers/ProductController.cs`): entity-level
  checks via `_contextAccessor.WorkContext.HasAccessToProduct(product)`,
  applied per-action rather than as a list filter.

## Architecture

A new abstraction in `Grand.Web.AdminShared`:

```csharp
public interface IAdminDataScope<TEntity>
{
    Task<bool> HasAccess(TEntity entity);
    IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query);
    string? DefaultStoreId { get; } // null for Admin/Vendor, StaffStoreId for Store
}
```

Three implementations, one per host, each registered in that host's own
`Startup` (matching how each host registers its own services today):

- `GlobalAdminDataScope<TEntity>` (Admin) — `HasAccess` always true, `ApplyScope`
  is a no-op, `DefaultStoreId` is `null`.
- `StoreAdminDataScope<TEntity>` (Store) — wraps `StaffStoreId` filtering/
  defaulting in one place instead of the current scattered call sites.
- `VendorAdminDataScope<TEntity>` (Vendor) — delegates to the existing
  `IWorkContext.HasAccessToProduct` (or the generalized equivalent).

`DefaultStoreId` exists specifically to make today's implicit per-host
default (Store always stamps `model.StoreId`; Admin never does) explicit and
testable instead of an artifact of the diff.

The interface is typed generically (`<TEntity>`) and named without a `Product`
suffix so Order/Category/Collection can implement it later, but this spec
only ships the `Product` instantiation and only what `ProductController`
actually needs — no speculative members beyond the three above.

## Phase 1 — Controller and service consolidation

- `Grand.Web.AdminShared/Controllers/BaseProductController.cs`: the union of
  today's three controllers' action logic, with every `StaffStoreId`/
  `HasAccessToProduct` call site replaced by calls into the injected
  `IAdminDataScope<Product>`.
- Three per-host `ProductController : BaseProductController` subclasses,
  reduced to `[Area(...)]` + constructor pass-through, matching
  `LoginController`.
- `Grand.Web.AdminShared/Services/ProductViewModelService.cs` gains whatever
  Vendor's copy has that AdminShared's doesn't. Each real difference found in
  the 1768-line diff must be attributed to a scope decision (`IAdminDataScope`)
  or ported as shared behavior — never copy-pasted as a parallel branch.
  Vendor's own `Services/ProductViewModelService.cs` is deleted; Vendor starts
  consuming AdminShared's, as Store already does.
- Existing characterization tests are the migration's correctness gate: they
  move to (or are consolidated into) `Grand.Web.AdminShared.Tests`, plus thin
  per-host tests that check only routing/authorization attributes. New unit
  tests cover the three `IAdminDataScope` implementations directly.
- Phase 1 ships as an independently mergeable, fully working change — no
  half-migrated state, no flag.

## Phase 2 — View consolidation

- `Product/*.cshtml` views (51 Admin / 51 Store / 49 Vendor) move to
  `Grand.Web.AdminShared/Views/Product/`.
- `Grand.Web.Common/View/ViewLocationExpander.cs` (today handles only the
  storefront `ThemeKey` case) gains an admin-area branch: when the executing
  controller derives from `BaseProductController`, AdminShared's view folder
  is added as a fallback location.
- Views whose only difference is the hardcoded area string
  (`Constants.AreaAdmin`/`AreaStore`/`AreaVendor`) are unified into one
  AdminShared view using the request's current area instead.
- Views with a real functional difference (e.g. Admin-only bulk export panel
  on `List.cshtml`) stay as host-specific overrides, resolved before the
  AdminShared fallback by the expander.
- Phase 2 depends on Phase 1 (needs `BaseProductController` to exist as the
  branch condition) but is its own mergeable unit with its own review
  checkpoint — work can pause between phases without leaving a broken or
  half-migrated state.

## Testing

- Phase 1: run and green all migrated/consolidated `ProductControllerTests`
  and `ProductViewModelServiceTests` (Admin/Store/Vendor), plus new
  `IAdminDataScope` unit tests.
- Phase 2: manual/characterization pass over rendered Product screens per
  host (List, Create, Edit, and the tabs/partials with known host-specific
  content) to confirm the expander resolves views correctly and overrides
  render where expected.

## Out of scope

- Any entity other than Product (Order, Category, Collection, etc.) — future
  work, enabled but not started by this spec.
- Merging the three hosts into a single deployable app — explicitly rejected
  in the ARCH-001 finding; auth models, data scopes, and independent
  deployability stay separate.
- A generalized `IAdminAreaContext` covering area name + capability flags —
  considered (design option C) and deferred as speculative beyond what
  Product needs today.
