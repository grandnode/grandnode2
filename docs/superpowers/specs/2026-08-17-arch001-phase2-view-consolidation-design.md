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

## Out of scope

- Automated view-rendering tests (`WebApplicationFactory`, Testcontainers-backed
  Mongo) — noted above as a real gap, but its own project; revisit separately
  if the manual pass proves too costly to repeat as Phase 3+ lands.
- Any entity other than Product.
- Merging the three hosts into one deployable app (same standing rejection as
  Phase 1).
