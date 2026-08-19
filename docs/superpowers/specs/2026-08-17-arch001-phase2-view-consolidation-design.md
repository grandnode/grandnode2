# ARCH-001 Phase 2 — Product view consolidation design

Date: 2026-08-17
Status: Approved, ready for implementation planning

## Problem

Phase 1 (`docs/superpowers/specs/2026-08-16-arch001-product-consolidation-design.md`,
implemented on branch `arch001/phase1-product-consolidation`, PR #790) consolidated
`ProductController` and `ProductViewModelService` into `Grand.Web.AdminShared`,
reducing each host's controller to a ~20-70 line subclass of `BaseProductController`.
The one duplication Phase 1 explicitly deferred is views: `Grand.Web.Admin`,
`Grand.Web.Store`, and `Grand.Web.Vendor` each still carry their own copy of
`Product/*.cshtml` (51 / 51 / 49 files, ~9300 / ~9060 / ~8600 lines). Spot-check
diff of `List.cshtml` (Admin vs Store) confirms the same pattern Phase 1 found in
controllers: some files differ only in a hardcoded area string or resource-key
prefix, others have a real functional difference (Admin's bulk export/import/
delete panel and `SearchStoreId`/`SearchVendorId` filters are absent from Store's
`List.cshtml`; Vendor lacks `CreateOrUpdate.Discounts.cshtml` and
`CreateOrUpdate.Documents.cshtml` entirely).

This spec covers **only the `Product` view set**, the second and final slice of
the Phase 1/2 split already anticipated in the Phase 1 spec's "Phase 2 — View
consolidation" section. It supersedes that section with a concrete, checked
design.

## Existing precedent

Plugins in this repo already compile Razor views into their own assembly and
have them discovered at runtime — e.g. `src/Plugins/DiscountRules.Standard/
DiscountRules.Standard.csproj` uses `Sdk="Microsoft.NET.Sdk.Razor"` with
`<AddRazorSupportForMvc>true</AddRazorSupportForMvc>`. Per project memory
`reference_running_the_storefront`, "Plugin views compile into the plugin DLL."
This is the same mechanism ASP.NET Core uses for Razor Class Libraries consumed
via `ProjectReference` (MSBuild auto-generates a `RelatedAssembly` attribute on
the consuming project, and `ApplicationPartManager` auto-discovers the
referenced assembly's compiled views) — no plugin-loading machinery is needed
for this case since `Grand.Web.AdminShared` is already a compile-time
`ProjectReference` from all three hosts.

`Grand.Web.Common/View/ViewLocationExpander.cs` already implements one
conditional, additive branch (`ThemeKey`, for storefront theme overrides). This
design adds a second, independent branch to the same class rather than
introducing a new expander.

## Goals

- Delete the ~150-file, ~27000-line-total duplication the same way Phase 1
  deleted the controller/service duplication: one canonical copy per view,
  living in `Grand.Web.AdminShared`, with host-specific overrides only where a
  real functional difference exists.
- No change to deployability: each host stays independently buildable and
  deployable; views arrive via the existing `ProjectReference`, not a new
  packaging or runtime-discovery mechanism.
- Host-specific views continue to render as they do today — this is a pure
  dedup, not a UX change (aside from the deliberate, already-known Store/Vendor
  feature gaps captured in Phase 1's controller work).

## Non-goals

- No new automated test infrastructure. This repo has no `WebApplicationFactory`
  usage anywhere (confirmed by search) and host startup is gated on
  `DataSettingsManager.DatabaseIsInstalled()`, meaning a real integration-test
  harness would need Mongo (e.g. Testcontainers) — a project of its own. Out of
  scope here; verification stays the manual/characterization pass the Phase 1
  spec already anticipated.
- No splitting of host-specific-difference views into shared skeleton + partial
  override. A view with a real functional difference stays a whole-file,
  host-specific override. Revisit only if a future file turns out to be >80%
  identical with one small differing block — decide per-file during migration,
  default to whole-file override (YAGNI).
- No change to non-Product views. Order/Category/Collection view consolidation
  is future work enabled, not started, by this design (same boundary Phase 1
  drew for controllers/services).
- No generalized `IAdminAreaContext` or view-model changes — this is a view
  file relocation plus one expander branch, nothing in `BaseProductController`
  or `ProductViewModelService` changes.

## Design

### 1. `Grand.Web.AdminShared` becomes a Razor Class Library

Change `src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <Import Project="..\..\Build\Grand.Common.props" />
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  </PropertyGroup>
  <!-- existing ItemGroups unchanged -->
</Project>
```

No other project in the solution needs to change how it references AdminShared
— the three hosts already have a `ProjectReference` to it from Phase 1.

### 2. View location: `Grand.Web.AdminShared/Views/Product/*.cshtml`

Not under an `Areas/` folder — AdminShared has no area of its own. The relative
path a Razor view compiles under (`/Views/Product/List.cshtml`) becomes its
lookup key application-wide, independent of which assembly compiled it.

### 3. `ViewLocationExpander` gets a second, independent branch

`src/Web/Grand.Web.Common/View/ViewLocationExpander.cs`, in
`ExpandViewLocations`:

```csharp
public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
    IEnumerable<string> viewLocations)
{
    if (context.Values.TryGetValue(ThemeKey, out _))
    {
        var viewFactory = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IViewFactory>();
        viewFactory.GetViewPath(context.AreaName ?? "", ref viewLocations);
    }

    if (IsAdminSharedController(context.ActionContext.ActionDescriptor))
        viewLocations = viewLocations.Append("/Views/{1}/{0}.cshtml");

    return viewLocations;
}

private static bool IsAdminSharedController(ActionDescriptor descriptor)
{
    if (descriptor is not ControllerActionDescriptor cad) return false;
    for (var t = cad.ControllerTypeInfo.AsType(); t is not null; t = t.BaseType)
        if (t.Namespace == "Grand.Web.AdminShared.Controllers")
            return true;
    return false;
}
```

Generic namespace check, not a hardcoded `BaseProductController` reference —
Phase 3 (Order, Category, ...) gets the fallback automatically the moment a
`Base*Controller` lands in that namespace, with zero further change to this
file. The `Append` (not prepend) is what makes host-specific overrides win:
`RazorViewEngine` tries each location in order and returns the first file that
exists, so a host's own `Areas/{Area}/Views/Product/X.cshtml` — which appears
earlier in the default location list — always wins over the AdminShared
fallback when both exist.

The two branches (`ThemeKey` / AdminShared) are independent and additive —
Grand.Web (storefront) has no `Grand.Web.AdminShared.Controllers`-derived
controllers, and Admin/Store/Vendor have no theme context, so in practice at
most one branch ever fires per request.

### 3a. Layout resolution (addendum, found while drafting the pilot task)

`List.cshtml`/`Create.cshtml`/`Edit.cshtml` (and most other Product views) set
no `Layout` themselves — they inherit it from each host's own
`Areas/{Area}/Views/_ViewStart.cshtml` (e.g.
`src/Web/Grand.Web.Admin/Areas/Admin/Views/_ViewStart.cshtml` sets
`Layout = Constants.Layout_Admin`). Razor's `_ViewStart.cshtml` discovery walks
up from the **resolved logical path the view was found under**, not from the
requesting controller's area. A view resolved through the new fallback
location (`/Views/Product/List.cshtml`, inside `Grand.Web.AdminShared`) walks
up `/Views/Product/` → `/Views/` → `/` looking for `_ViewStart.cshtml` there —
it never sees `/Areas/Admin/Views/_ViewStart.cshtml`, so `Layout` would be left
unset and the page would render with no host chrome.

Fix: add `src/Web/Grand.Web.AdminShared/Views/_ViewStart.cshtml`:

```cshtml
@{
    var area = Context.GetRouteValue("area")?.ToString();
    Layout = $"~/Areas/{area}/Views/Shared/_{area}Layout.cshtml";
}
```

The three hosts' layout files already follow this exact naming convention
(`_AdminLayout.cshtml`, `_StoreLayout.cshtml`, `_VendorLayout.cshtml`, confirmed
via `Constants.Layout_Admin`/`LayoutStore`/`LayoutVendor` in each host's own
`Extensions/Constants.cs`) and stay put in each host — only `Views/Product/*`
moves. The `~/`-rooted path resolves against the full merged view-location
provider (all `ApplicationPart`s, including the executing host's own compiled
views), so it finds the host's own layout correctly regardless of which
assembly the `_ViewStart.cshtml` itself lives in.

### 3b. `_ViewImports.cshtml` for the shared view folder (addendum)

Each host's `Areas/{Area}/Views/_ViewImports.cshtml` brings in the tag helpers
and `@inject`s a migrated view needs (`Loc` for resource lookups,
`EnumTranslationService`). The tag helpers Product views actually use
(`admin-input`, `admin-select`, `admin-label`, etc.) come from
`@addTagHelper *, Grand.Web.Common` — already common to all three hosts'
`_ViewImports.cshtml`, not from any host-specific tag helper assembly — so a
single shared import file covers them. Add
`src/Web/Grand.Web.AdminShared/Views/_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Grand.Web.Common

@using System.Globalization
@using Microsoft.AspNetCore.Http.Extensions
@using Microsoft.AspNetCore.Mvc.ViewFeatures
@using System.Text
@using Grand.SharedKernel.Extensions
@using Grand.Infrastructure
@using Grand.Domain.Common
@using Grand.Domain.Catalog
@using Grand.Domain.Directory
@using Grand.Web.Common
@using Grand.Web.Common.Extensions
@using Grand.Web.Common.Localization
@using Grand.Web.AdminShared.Models.Catalog
@using Grand.Web.AdminShared.Interfaces

@inject LocService Loc
@inject IEnumTranslationService EnumTranslationService
@inject IAdminDataScope<Product> Scope
```

Injecting `Scope` at the `_ViewImports` level (not per-file) means every
migrated view gets `Scope.ResourceKeyPrefix` (for the `Admin.*`/`Vendor.*`
resource-key split, same as `BaseProductController`'s Phase 1 pattern) and
`ViewContext.RouteData.Values["area"]` (read per-file where an
`asp-area="@Constants.AreaAdmin"` literal needs replacing) without repeating
the `@inject` line in all ~50 files. If a per-file migration needs a tag
helper or using not in this list, add it here rather than to the individual
file, unless it's genuinely single-file-specific.

### 4. Per-file migration classification

For each of the ~53 distinct Product view filenames (union of the three
hosts), read all present variants and classify:

| Case | Resolution |
|---|---|
| Byte-identical, or differs only in a hardcoded area string / resource-key prefix already unified behind `IAdminDataScope` in Phase 1 | One file in `AdminShared/Views/Product/`, using `ViewContext.RouteData.Values["area"]` (or the equivalent existing helper) instead of a literal `Constants.AreaAdmin`/`AreaStore`/`AreaVendor`; delete the 2-3 host copies. |
| Differs only by a capability flag Phase 1 already introduced (e.g. `Model.ShowStoreSelector`, `scope.ResourceKeyPrefix`) | One file with the existing conditional (`@if (Model.ShowStoreSelector) { ... }`); delete host copies. |
| Real functional difference (Admin-only bulk export/import/delete panel and store/vendor search filters on `List.cshtml`; Vendor missing `Discounts`/`Documents` partials entirely) | Stays as a whole-file, host-specific override in that host's own `Areas/{Area}/Views/Product/` folder. Not moved to AdminShared. |

This mirrors Phase 1's Task 8/10 discipline: one file (or a tightly-coupled
small group, e.g. `CreateOrUpdate.*.cshtml` region partials with a shared
parent) per checklist row, each read across all present hosts, classified,
migrated, and committed independently — subagent-driven-development, one
subagent per row.

### 5. Verification

- `dotnet build GrandNode.sln` after the RCL conversion and after each
  migration batch — a missing view at runtime is a startup-time or
  render-time failure, not a compile error, so build success alone is not
  sufficient evidence.
- Manual/characterization pass per host (per the original Phase 1 spec's
  Testing section): List → Create → Edit → Save for an existing product,
  once per host (Admin/Store/Vendor), confirming the page renders with the
  expected host-specific content (or lack thereof) and no
  `InvalidOperationException: The view '...' was not found` error.
- Existing MSTest suites (`Grand.Web.Admin.Tests`, `Grand.Web.Store.Tests`,
  `Grand.Web.Vendor.Tests`) stay green throughout — they don't render Razor
  views today (confirmed: no `WebApplicationFactory` usage in the repo), so
  they are a regression guard for the controller/service layer this touches
  incidentally (e.g. if a `.cshtml` move breaks a `[ViewComponent]` or model
  binding), not a substitute for the manual pass above.

### 4a. Widget-zone selection: per-area partial files, not an inline `@if` (addendum, 2026-08-18)

Early Task 4 batches unified views containing a widget-zone tag-helper call
(`<vc:admin-widget widget-zone="X" .../>` vs `<vc:vendor-widget
widget-zone="vendor_X" .../>`) using an inline conditional:

```cshtml
@if (Scope.ResourceKeyPrefix == "Vendor")
{
    <vc:vendor-widget widget-zone="vendor_product_bulk_edit_buttons" additional-data="null"/>
}
else
{
    <vc:admin-widget widget-zone="product_bulk_edit_buttons" additional-data="null"/>
}
```

Superseded. Widget-zone selection now uses a small, per-occurrence partial
resolved through the same host-override-wins mechanism section 3 already
established, instead of a C# branch inside the unified file:

- `src/Web/Grand.Web.AdminShared/Views/Product/Partials/WidgetZone.<Name>.cshtml`
  holds the Admin/Store-shared default: `<vc:admin-widget widget-zone="X"
  additional-data="..."/>`.
- `src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product/Partials/WidgetZone.<Name>.cshtml`
  holds Vendor's override: `<vc:vendor-widget widget-zone="vendor_X"
  additional-data="..."/>`.
- The parent unified view calls `<partial name="Partials/WidgetZone.<Name>"
  model="..."/>` in place of the old `@if` block. Admin and Store naturally
  fall through to the AdminShared default (no host-specific file needed for
  them, since they share it); Vendor's own `Areas/Vendor/...` copy is found
  first by `RazorViewEngine` and wins, exactly like any other host override
  under section 3 — no new expander logic needed.
- `<Name>` is a short, occurrence-specific PascalCase name derived from the
  zone-name pair with the common `product_`/`vendor_product_` prefix and any
  `vendor_` prefix stripped (e.g. `product_bulk_edit_buttons` /
  `vendor_product_bulk_edit_buttons` → `WidgetZone.BulkEditButtons.cshtml`;
  `product_details_bids_top` / `vendor_product_details_bids_top` →
  `WidgetZone.Bids.Top.cshtml`). Pick a name that reads clearly next to its
  sibling occurrences in the same parent file (e.g. `Bids.Top`/`Bids.Bottom`
  for the two zones inside `CreateOrUpdate.Bids.cshtml`).

Rationale: `Scope.ResourceKeyPrefix`-branching was already established for
genuinely mixed content (a whole tab present in some hosts, per section 3's
`CreateOrUpdate.cshtml` case) where no other mechanism fits cleanly. For
widget-zone selection specifically — a single self-contained tag-helper call
repeated at ~20+ sites — a per-area file keeps each host's markup physically
separate and lets the existing view-resolution precedence do the selection,
rather than growing every unified file's branch count. This also means a
future widget-zone-only change to one host never touches the shared parent
file at all.

All files already unified under the old inline-`@if` pattern get retrofitted
to this one in the same implementation pass that introduces it (tracked in
the plan's Task 4 as a one-time batch), so the codebase never carries both
patterns side by side once that pass lands.

## Out of scope

- Automated view-rendering tests (`WebApplicationFactory`, Testcontainers-backed
  Mongo) — noted above as a real gap, but its own project; revisit separately
  if the manual pass proves too costly to repeat as Phase 3+ lands.
- Any entity other than Product.
- Merging the three hosts into one deployable app (same standing rejection as
  Phase 1).

## 5. Post-review corrections (2026-08-18)

A final whole-branch review inspected the *compiled* assemblies and the combined
`Grand.Web` host, and found two render-time regressions that neither the diff nor
`dotnet build` could show. Both are fixed on this branch. **Sections 2, 3a, 3b and
4a above are superseded on the two points below** — read this section as the
current truth.

### 5.1 Shared views live under `/Views/AdminShared/…`, not `/Views/…` (supersedes section 2)

Section 2 asserted that a view's compiled path "becomes its lookup key
application-wide" without checking that key against existing occupancy. A Razor
view path is global across *every* `ApplicationPart`, and `Grand.Web` — the
combined host — references `Grand.Web.{Admin,Store,Vendor}` and therefore
transitively loads AdminShared's views alongside its own storefront views. Two
real collisions existed: `/Views/_ViewStart.cshtml` (AdminShared's admin-layout
resolver vs. the storefront's `Layout = "_Layout"`) and
`/Views/Product/Partials/ProductAttributes.cshtml` (admin `ProductModel` partial
vs. the storefront product-details partial). One silently shadows the other and
the loser fails at render time; which one wins depends on application-part order.

Corrected layout:

- `src/Web/Grand.Web.AdminShared/Views/AdminShared/{Controller}/*.cshtml`
- `src/Web/Grand.Web.AdminShared/Views/AdminShared/_ViewImports.cshtml`
- `src/Web/Grand.Web.AdminShared/Views/AdminShared/_ViewStart.cshtml`
- `ViewLocationExpander.AdminSharedFallbackLocation` = `/Views/AdminShared/{1}/{0}.cshtml`

The `_ViewStart` ancestor walk still resolves
(`/Views/AdminShared/Product/X.cshtml` → `/Views/AdminShared/_ViewStart.cshtml`)
and host-override precedence is unchanged.

**Rule for Phase 3 and later:** every shared entity folder goes under
`Views/AdminShared/`. The `AdminShared` segment is owned by no other project, so
`Order/`, `Vendor/`, `Page/`, `Blog/`, `News/` and `Catalog/` — all of which
already exist under `src/Web/Grand.Web/Views/` — cannot collide.

### 5.2 Widget-zone defaults live in `Grand.Web.Admin`, not AdminShared (amends section 4a)

Section 4a placed the Admin/Store-shared widget-zone default in
`AdminShared/Views/Product/Partials/WidgetZone.<Name>.cshtml`. That does not
work: `<vc:admin-widget>` binds to `AdminWidgetViewComponent` in
`Grand.Web.Admin`, and Razor binds tag helpers **at compile time** from the
compiling project's `@addTagHelper` set. `Grand.Web.AdminShared` only adds
`Grand.Web.Common`, so all 44 widget-zone calls in the moved views compiled to
literal `<vc:admin-widget …/>` markup — no compile error, no warning, no runtime
exception, just dead zones and a stray custom element per site. Verified in the
built assembly: 44 literal `<vc:` strings in `Grand.Web.AdminShared.dll` and zero
`AdminWidgetViewComponent` references.

Corrected placement, per widget zone:

| File | Content |
|---|---|
| `Grand.Web.Admin/Areas/Admin/Views/Product/Partials/WidgetZone.<Name>.cshtml` | the real `<vc:admin-widget widget-zone="X" …/>` |
| `Grand.Web.Vendor/Areas/Vendor/Views/Product/Partials/WidgetZone.<Name>.cshtml` | the real `<vc:vendor-widget widget-zone="vendor_X" …/>` (unchanged) |
| `Grand.Web.AdminShared/Views/AdminShared/Product/Partials/WidgetZone.<Name>.cshtml` | an empty `@* … *@` placeholder, which Store falls through to |

The parent shared view still calls `<partial name="Partials/WidgetZone.<Name>"
model="…"/>`; selection still happens purely through the section-3 override
precedence. The empty AdminShared placeholder preserves Store's exact
pre-existing behaviour (Store has no widget component and never rendered these
zones) while making it deliberate instead of accidental.

**Rule:** a Razor construct that binds to a *host's* tag helper or view component
cannot live in `Grand.Web.AdminShared`. Only markup whose tag helpers come from
`Grand.Web.Common` is shareable. The cheap mechanical check is that
`Grand.Web.AdminShared.dll` must contain zero literal `<vc:` strings.

The two files that carried inline widget calls
(`CreateOrUpdate.Discounts.cshtml`, `CreateOrUpdate.Documents.cshtml`) stay in
AdminShared — Store needs both tabs — with the calls extracted into
`WidgetZone.{Discounts,Documents}.{Top,Bottom}` pairs. Their previously deferred
hardcoded `Loc["Admin.…"]` keys were templated to
`Loc[$"{Scope.ResourceKeyPrefix}.…"]` in the same pass.

### 5.3 Other corrections in the same wave

- `RoutedProductDataScope` now fails **closed**: `"Admin"` is an explicit arm and
  an unrecognized or missing `area` throws `InvalidOperationException` instead of
  resolving to the unscoped `GlobalAdminDataScope`. Covered by new
  `RoutedProductDataScopeTests` in `Grand.Web.Admin.Tests`.
- `AdminShared/Views/AdminShared/_ViewImports.cshtml` documents the two
  host-detection idioms (`Scope.ResourceKeyPrefix` for Admin+Store vs. Vendor;
  the `area` route value when Admin must be told apart from Store) so later
  phases do not add a third.
- `_ViewStart.cshtml` guards a null/empty `area` instead of composing
  `~/Areas//Views/Shared/_Layout.cshtml`.

### 5.4 Still outstanding

The manual per-host smoke pass from the Design section's "5. Verification" has **not** been run. It must cover
all three standalone hosts *and* the combined `grand-web` host, and must be run
against a non-`Development` environment — `AddRazorRuntimeCompilation()` is
enabled only when `ASPNETCORE_ENVIRONMENT == Development`, so a Development run
resolves views differently from a production build.

## 6. Retire `Scope.ResourceKeyPrefix != "Vendor"` as a content-gating idiom (addendum, 2026-08-18)

### Problem

Section 4a/5.2 replaced widget-zone `@if (Scope.ResourceKeyPrefix == "Vendor")` branches with
per-host partial files. Six sites doing the same string comparison for a different purpose —
hiding or altering a block of markup for Vendor, not selecting a widget component — were left
behind:

- `Views/AdminShared/Product/Partials/CreateOrUpdate.cshtml:7,132` — gates the Documents tab
  (combined with a permission check) and the UserFields tab.
- `Views/AdminShared/Product/Partials/CreateOrUpdateTierPrice.cshtml:31` — gates the
  Store/CustomerGroup form-group pair.
- `Views/AdminShared/Product/Partials/CreateOrUpdate.Additional.cshtml:71` — gates the whole
  `group-downloads` section.
- `Views/AdminShared/Product/Partials/CreateOrUpdate.Categories.cshtml:100-107,109-122` and the
  equivalent lines in `CreateOrUpdate.Collections.cshtml` — one branch swaps a Kendo Grid
  `template:` between a clickable link (Admin/Store) and plain text (Vendor); a second branch
  adds/omits the whole `IsFeaturedProduct` column definition.
- `Grand.Web.AdminShared/Services/ProductViewModelService.cs:577` — C#, not Razor; gates the
  "Show on homepage" search-filter option. Already flagged by a comment left on that line in the
  prior pass recommending exactly the fix below.

`ResourceKeyPrefix` is documented (Interfaces/IAdminDataScope.cs) as a localization-key-building
string ("Admin" for Global and Store, "Vendor" for Vendor) — using it as a two-way behavior switch
overloads a property with one declared purpose to also do another, undeclared one. It works only
by accident (today there are exactly two distinct prefix values, and they happen to line up with
the Vendor/non-Vendor split every one of these six sites needs).

A companion check confirmed `Admin.*`/`Vendor.*` resource values under `catalog.products.*` are
byte-identical wherever both exist (0 of ~230 shared keys differ; the only 7 differing pairs in the
whole resource file are under `reports.*`, outside this design's scope). No change to the resource
files follows from this — deleting or merging `Vendor.*` entries would need an upgrade migration
(they're seeded to Mongo, not read live from XML) and would silently drop any store owner's custom
translations of those keys. Out of scope here. `ResourceKeyPrefix` keeps its one declared job:
building `Loc[$"{Scope.ResourceKeyPrefix}...."]` keys. Nothing branches on it anymore after this
section.

### Resolution, per site

Same override-wins mechanism as section 4a, applied to three shapes of content difference:

1. **Presence/absence of a self-contained block** (a `<tabstrip-item>`, a `<div class="form-group">`
   pair, a whole `<div id="group-...">` section, a Kendo column-definition object): extract the
   block into `Partials/<Name>.cshtml`, called unconditionally from the parent. AdminShared's copy
   holds the real content (Admin+Store); a same-named file under
   `Grand.Web.Vendor/Areas/Vendor/Views/Product/Partials/<Name>.cshtml` is empty (an `@* ... *@`
   comment, mirroring the empty-placeholder precedent from section 5.2) and wins for Vendor through
   ordinary view-location precedence. No C# conditional remains in the parent file.
   - `CreateOrUpdate.cshtml` → `Partials/Tab.Documents.cshtml`, `Partials/Tab.UserFields.cshtml`
     (Documents additionally still needs the `ManageDocuments` permission check — that check moves
     into the AdminShared partial itself, since it's an authorization concern, not a host-shape
     one; Vendor's empty override doesn't need it, it never had the tab).
   - `CreateOrUpdateTierPrice.cshtml` → `Partials/TierPrice.StoreScope.cshtml`.
   - `CreateOrUpdate.Additional.cshtml` → `Partials/Additional.Downloads.cshtml`.
   - `CreateOrUpdate.Categories.cshtml` / `CreateOrUpdate.Collections.cshtml` (the
     `IsFeaturedProduct` column) → `Partials/Categories.FeaturedColumn.cshtml` /
     `Partials/Collections.FeaturedColumn.cshtml`.
2. **Content that differs rather than disappears** (the Kendo `template:` link-vs-plain-text
   swap): extract just the differing fragment into its own partial, both hosts get a real file.
   - `CreateOrUpdate.Categories.cshtml` → `Partials/Categories.LinkTemplate.cshtml` (AdminShared:
     `template: '<a class="k-link" href="...">#:Category#</a>'`; Vendor:
     `template: '#:Category#'`).
   - `CreateOrUpdate.Collections.cshtml` → `Partials/Collections.LinkTemplate.cshtml`, same shape.
   - `CreateOrUpdate.Reviews.cshtml` (found during implementation - not in the original six-site
     audit, which only searched for `!= "Vendor"` and missed this file's `== "Vendor"` phrasing of
     the same idiom) → `Partials/Reviews.CustomerLinkTemplate.cshtml` and
     `Partials/Reviews.TitleLinkTemplate.cshtml`, same shape, two occurrences in one file.
3. **C# capability gate** (no partial mechanism applies outside Razor): add a named boolean to
   `IAdminDataScope<TEntity>` instead of comparing `ResourceKeyPrefix`, matching the pattern
   `ShowStoreSelector`/`DefaultVendorId` already establish for capability flags.
   - Add `bool CanFeatureOnHomepage { get; }` — `true` on `GlobalAdminDataScope<TEntity>` and
     `StoreAdminDataScope<TEntity>`, `false` on `VendorProductDataScope`.
   - `ProductViewModelService.cs:577` becomes `if (scope.CanFeatureOnHomepage)`, and the comment
     explaining the old string-comparison workaround is deleted (the workaround it warned about is
     gone).

### Verification

- `dotnet build GrandNode.sln`.
- `Grand.Web.AdminShared.dll` still contains zero literal `<vc:` strings (section 5.2's check) —
  unaffected by this section, listed here only as a standing regression guard for the same class of
  compile-time tag-helper-binding mistake.
- Manual pass: open a product's Edit page as Admin, Store owner, and Vendor. Confirm Vendor sees no
  Documents/UserFields tabs, no Store/CustomerGroup tier-price fields, no downloads section, no
  "Show on homepage" filter option, and Category/Collection grids show plain text instead of links;
  confirm Admin and Store are pixel-identical to their pre-change rendering.
- `Grand.Web.Admin.Tests` / `Grand.Web.Store.Tests` / `Grand.Web.Vendor.Tests` stay green — the new
  `CanFeatureOnHomepage` flag is a small, direct addition to the existing
  `GlobalAdminDataScopeTests`/`StoreAdminDataScopeTests`/`VendorProductDataScopeTests` fixtures.

## 7. Wire up Store's own `vc:store-widget` for Product widget zones (addendum, 2026-08-18)

### Problem

Section 5.2 placed an empty AdminShared placeholder at every one of the 44
`WidgetZone.<Name>.cshtml` files with the comment "Store has no equivalent widget zone." That claim
was checked against Store's *original* (pre-Phase-2) Product views, which is where it went wrong:
those original files called `<vc:admin-widget widget-zone="product_...".../>` — copy-pasted from
Admin without adapting it — and Store's `_ViewImports.cshtml` never added `@addTagHelper *,
Grand.Web.Admin`, so that tag helper was already dead literal markup in Store's own pre-migration
code (confirmed via `git show` on the pre-migration commit). Section 5.2's placeholder therefore
preserved a genuine pre-existing bug rather than the intended behavior: `Grand.Web.Store` has its
own `StoreWidgetViewComponent` (`Grand.Web.Store/Components/StoreWidget.cs`), registered as
`vc:store-widget`, actively used elsewhere in Store's own views
(`Areas/Store/Views/Home/Index.cshtml`, `Statistics.cshtml`, `Shared/_StoreLayout.cshtml`) with a
`store_`-prefixed zone-name convention (`store_dashboard_top`, `store_header_before`, ...) — it was
simply never wired into Product's widget zones, likely since the Store panel's Product screens were
first added.

### Resolution

Same three-tier table as section 5.2, extended with Store's own row - Store gets the same treatment
Vendor already has, using its own established `store_` prefix (mirrors `vendor_product_X`):

| File | Content |
|---|---|
| `Grand.Web.Admin/Areas/Admin/Views/Product/Partials/WidgetZone.<Name>.cshtml` | `<vc:admin-widget widget-zone="product_X" .../>` (unchanged) |
| `Grand.Web.Store/Areas/Store/Views/Product/Partials/WidgetZone.<Name>.cshtml` | **new:** `<vc:store-widget widget-zone="store_product_X" .../>` |
| `Grand.Web.Vendor/Areas/Vendor/Views/Product/Partials/WidgetZone.<Name>.cshtml` | `<vc:vendor-widget widget-zone="vendor_product_X" .../>` (unchanged, 40 of 44 - Vendor has no Discounts/Documents tabs) |
| `Grand.Web.AdminShared/Views/AdminShared/Product/Partials/WidgetZone.<Name>.cshtml` | empty placeholder (unchanged content, comment corrected - now genuinely unreachable except as the Discounts/Documents fallback for Vendor) |

All 44 zones apply to Store (Store shows the same tabs as Admin; only Vendor's 4 gaps differ), so
Store gets all 44, generated mechanically from Admin's 44 real files by substituting
`vc:admin-widget` → `vc:store-widget` and prefixing each `widget-zone` value with `store_` - the
same transform already proven correct by Vendor's `vendor_` prefix. New zone names
(`store_product_...`) are brand new; no widget plugin currently targets them, so this changes no
visible behavior today - it makes the extension point reachable for future (or existing,
not-yet-Product-scoped) Store widget plugins, exactly like Vendor's equivalent zones were reachable
but likely unpopulated when they were added.

The three pre-existing Store-specific whole-file overrides kept outside AdminShared per section 4
(`CreateOrUpdate.{Info,Prices,PurchasedWithOrders}.cshtml`, kept because each has a real grid/column
difference from Admin) carried the same dead `vc:admin-widget` copy-paste and got the same fix in
the same pass, since they're Store's own files either way.

`Grand.Web.AdminShared.dll` keeps zero literal `<vc:` strings (unaffected - this section only adds
real widget calls in `Grand.Web.Store`, never in AdminShared). `Grand.Web.Store.dll` gains real
`StoreWidgetViewComponent` references in place of what would otherwise have been dead literal
`<vc:store-widget>` markup, checked the same way section 5.2 checked Admin/Vendor.

**Rule for Phase 3:** when checking "does host X have an equivalent mechanism" during a
consolidation, check host X's *own* codebase for the real answer (does it have a widget component,
is it used elsewhere, what does `_ViewImports.cshtml` actually import) - don't infer it from what
the entity's *original*, pre-consolidation views for that host happened to contain. A copy-pasted,
never-adapted call is evidence of a pre-existing bug, not evidence the host lacks the capability.

## 8. Remove `IAdminDataScope<TEntity>.ApplyScope` (addendum, 2026-08-19)

### Problem

`ApplyScope(IQueryable<TEntity> query)` was part of the original Phase 1 interface design but never
became load-bearing: `BaseProductController` and `ProductViewModelService` scope every read
(`SearchProducts`) and write path through the `storeId`/`vendorId` parameters and `HasAccess`/
`CanView` checks instead, never through an `IQueryable` filter. Confirmed by grep across `src/Web`
and `src/Tests`: the only callers of `ApplyScope` were its own unit tests
(`GlobalAdminDataScopeTests.ApplyScope_ReturnsQueryUnchanged`,
`VendorProductDataScopeTests.ApplyScope_FiltersToOwnVendorId`) - dead production code advertising a
scoping mechanism nothing uses, flagged during the tenant-isolation audit in section 6/7's session
and removed on request rather than left to accumulate.

### Resolution

Removed the member from `IAdminDataScope<TEntity>` and its four implementations
(`GlobalAdminDataScope<TEntity>`, `StoreAdminDataScope<TEntity>`, `VendorProductDataScope`,
`RoutedProductDataScope`'s pass-through), plus the two tests that only existed to exercise it. No
other code referenced it (grep clean after removal). `IStoreLinkEntity`/`Stores`/`LimitedToStores`
filtering logic that lived inside `StoreAdminDataScope.ApplyScope` is not reproduced elsewhere - it
was never called, so there is nothing to preserve.

**If Phase 3 needs query-level scoping** (e.g. a list endpoint that filters via `IQueryable` instead
of passing a `storeId`/`vendorId` parameter into a service method, the way Product does), add the
member back on the entity/host where it is actually wired to a caller in the same change - not
speculatively ahead of a caller, which is what happened here.

### Verification

`dotnet build GrandNode.sln` clean. `Grand.Web.Admin.Tests`/`Grand.Web.Store.Tests`/
`Grand.Web.Vendor.Tests`: 419/33/8 (down 2 from the removed `ApplyScope` tests), all green.

## 9. CodeQL: "missing CSRF token validation" on `BaseProductController.cs` (addendum, 2026-08-19)

### Investigation

CodeQL flagged `BaseProductController.cs`'s `[HttpPost]` actions as missing antiforgery validation.
Checked: `BaseProductController` is `abstract` (never directly routable) and extends
`Grand.Web.Common.Controllers.BaseController`, which carries no antiforgery attribute. Its three
concrete subclasses (`Grand.Web.Admin`/`Grand.Web.Store`/`Grand.Web.Vendor`'s `ProductController`)
each already declare `[AutoValidateAntiforgeryToken]` at the class level - restated there since Phase
1 Task 11 explicitly because `BaseProductController` "can't inherit any single host's base controller"
(each host's own `BaseAdminController`/`BaseStoreController`/`BaseVendorController`, which normally
supplies it, differs by `[Area]`/`[Authorize*]`). ASP.NET Core resolves MVC filters from the full type
hierarchy of the concrete controller at request time, so every actual runtime endpoint (there are only
these three concrete subclasses - grep confirmed) is already protected. This is a static-analysis
false positive in the sense that no exploitable gap exists today - CodeQL's query doesn't follow an
attribute from a derived class in a different project back onto the base class where the actions are
textually defined.

### Fix

Added `[AutoValidateAntiforgeryToken]` directly to `BaseProductController` too. This changes no
runtime behavior (redundant with the three subclasses' own copies, and the base class was never
routable anyway), but removes a real fragility the false-positive investigation surfaced: protection
depended entirely on every current *and future* host subclass remembering to restate the attribute,
with nothing enforcing it at the point where the actions actually live. Also gives CodeQL's static
analysis something to see in the same file it flagged.

### Verification

`dotnet build GrandNode.sln` clean. `Grand.Web.Admin.Tests`/`Grand.Web.Store.Tests`/
`Grand.Web.Vendor.Tests`: 419/33/8, unchanged, all green.
