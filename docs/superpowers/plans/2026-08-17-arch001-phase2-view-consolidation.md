# ARCH-001 Phase 2 (View Consolidation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Task 4 is a per-file checklist (one `.cshtml` filename per row) — when using subagent-driven-development, dispatch one subagent per checklist row, not one subagent for the whole task.

**Goal:** Delete the three duplicated copies of `Product/*.cshtml` (51 Admin /
51 Store / 49 Vendor files) by moving one canonical copy of each into
`Grand.Web.AdminShared`, discovered at runtime via a `ViewLocationExpander`
fallback, with host-specific overrides kept only where a real functional
difference exists.

**Architecture:** `Grand.Web.AdminShared` becomes a Razor Class Library
(`Sdk="Microsoft.NET.Sdk.Razor"`, `AddRazorSupportForMvc=true`) — the same
mechanism this repo's plugins already use to ship views inside their own DLL.
`Grand.Web.Common/View/ViewLocationExpander.cs` gets a second, independent
branch: when the executing controller derives from a type in
`Grand.Web.AdminShared.Controllers`, it appends `/Views/{1}/{0}.cshtml` to the
end of the candidate view locations, so a host's own override (checked first
by `RazorViewEngine`) always wins over the AdminShared fallback.

**Tech Stack:** ASP.NET Core MVC Razor views, C# 13, MSTest (existing test
stack, unaffected by this plan — no `.cs` production logic changes, only
`.cshtml` moves and one `.csproj`/one `.cs` file).

**Spec:** `docs/superpowers/specs/2026-08-17-arch001-phase2-view-consolidation-design.md`

## Global Constraints

- No new automated view-rendering test infrastructure (spec, "Non-goals") —
  verification is `dotnet build` plus a manual/characterization pass.
- A view with a real functional difference between hosts stays a whole-file,
  host-specific override — never split into shared-skeleton-plus-partial
  (spec, "Non-goals" — YAGNI unless a specific file proves otherwise during
  Task 4, decided per-file, not planned in advance).
- Every moved view must lose its hardcoded `asp-area="@Constants.AreaAdmin"`-
  style literal (host-specific `Constants` class, not visible from
  `Grand.Web.AdminShared`) in favor of `ViewContext.RouteData.Values["area"]`,
  and every moved resource-key lookup that differs by host
  (`Admin.*`/`Vendor.*`) must route through the injected
  `Scope.ResourceKeyPrefix` (spec, sections 3, 3b).
- Only `Product` views. No other entity's views move in this plan.
- Follow existing repo conventions: `.ai/standards/razor-frontend.md` for
  Razor/tag-helper conventions, `.ai/skills/admin-area-changes.md` for
  admin-facing view changes.

---

## Task 0: Baseline

**Files:** none — verification only.

- [ ] **Step 1: Confirm the branch builds and tests pass before touching views**

Run:
```
dotnet build GrandNode.sln
dotnet test src/Tests/Grand.Web.Admin.Tests
dotnet test src/Tests/Grand.Web.Store.Tests
dotnet test src/Tests/Grand.Web.Vendor.Tests
```
Expected: Build succeeded, all tests PASS. If anything fails here, stop and
fix or report before starting Task 1 — this is the safety net Phase 1 left in
place (per project memory `project_arch001_triple_admin_duplication`, Phase 1
verified green on 2026-08-17).

- [ ] **Step 2: Record the current per-host view file counts**

Run:
```
find src/Web/Grand.Web.Admin/Areas/Admin/Views/Product -iname "*.cshtml" | wc -l
find src/Web/Grand.Web.Store/Areas/Store/Views/Product -iname "*.cshtml" | wc -l
find src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product -iname "*.cshtml" | wc -l
```
Expected: 51, 51, 49 — matches the spec's baseline. If different, the repo has
moved since this plan was written; stop and reconcile the plan's Task 4
checklist against the actual current file list before proceeding.

---

## Task 1: `Grand.Web.AdminShared` becomes a Razor Class Library

**Files:**
- Modify: `src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj`

**Interfaces:** none — no consumers exist yet (no `.cshtml` files added until
Task 3).

- [ ] **Step 1: Change the SDK and add Razor-for-MVC support**

Current file:
```xml
﻿<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\..\Build\Grand.Common.props" />
  <PropertyGroup>
	<ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  ...
```

Change the opening `<Project>` tag and `<PropertyGroup>`:
```xml
﻿<Project Sdk="Microsoft.NET.Sdk.Razor">
  <Import Project="..\..\Build\Grand.Common.props" />
  <PropertyGroup>
	<ImplicitUsings>enable</ImplicitUsings>
	<AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  </PropertyGroup>
  ...
```
Leave the rest of the file (the `ProjectReference`/`PackageReference`
`ItemGroup`s) unchanged.

- [ ] **Step 2: Build to confirm the SDK swap alone doesn't break anything**

Run:
```
dotnet build src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj
dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```
Expected: all succeed. `Sdk="Microsoft.NET.Sdk.Razor"` with no `.cshtml` files
present yet just adds Razor tooling to the build; it does not require any view
to exist.

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj
git commit -m "Convert Grand.Web.AdminShared to a Razor Class Library (ARCH-001 Phase 2)"
```

---

## Task 2: `ViewLocationExpander` fallback branch

**Files:**
- Modify: `src/Web/Grand.Web.Common/View/ViewLocationExpander.cs`
- Test: `src/Tests/Grand.Web.Common.Tests/View/ViewLocationExpanderTests.cs` (new file)

**Interfaces:**
- Produces: `ViewLocationExpander.ExpandViewLocations` gains a second,
  independent branch. The classification logic is extracted as an internal
  static method `IsAdminSharedController(ActionDescriptor)` so it's unit
  testable without a full `ViewLocationExpanderContext`.

- [ ] **Step 1: Check the existing test project's conventions**

Run: `find src/Tests/Grand.Web.Common.Tests -iname "*.cs" | head -5` to confirm
the project exists and see its namespace/test-attribute conventions (MSTest,
per `.ai/knowledge/tests.md`). If `View/` doesn't exist as a subfolder yet,
create it alongside the test file in Step 2.

- [ ] **Step 2: Write the failing tests**

```csharp
using Grand.Web.Common.View;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.View;

// Dummy hierarchy standing in for BaseProductController living in
// Grand.Web.AdminShared.Controllers — this test project doesn't reference
// AdminShared, so the namespace string itself is what's under test, not a
// real base type.
namespace Grand.Web.AdminShared.Controllers
{
    public abstract class FakeBaseController { }
}

public class FakeAdminSharedSubclass : Grand.Web.AdminShared.Controllers.FakeBaseController { }

public class FakeUnrelatedController { }

[TestClass]
public class ViewLocationExpanderTests
{
    private static ControllerActionDescriptor DescriptorFor(Type controllerType) =>
        new() { ControllerTypeInfo = controllerType.GetTypeInfo() };

    [TestMethod]
    public void IsAdminSharedController_TypeDerivesFromAdminSharedControllersNamespace_ReturnsTrue()
    {
        var descriptor = DescriptorFor(typeof(FakeAdminSharedSubclass));
        Assert.IsTrue(ViewLocationExpander.IsAdminSharedController(descriptor));
    }

    [TestMethod]
    public void IsAdminSharedController_UnrelatedType_ReturnsFalse()
    {
        var descriptor = DescriptorFor(typeof(FakeUnrelatedController));
        Assert.IsFalse(ViewLocationExpander.IsAdminSharedController(descriptor));
    }

    [TestMethod]
    public void IsAdminSharedController_NonControllerActionDescriptor_ReturnsFalse()
    {
        var descriptor = new ActionDescriptor();
        Assert.IsFalse(ViewLocationExpander.IsAdminSharedController(descriptor));
    }
}
```

Note: `ControllerTypeInfo.GetTypeInfo()` requires `using System.Reflection;` —
add it to the test file's usings.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test src/Tests/Grand.Web.Common.Tests --filter "FullyQualifiedName~ViewLocationExpanderTests"`
Expected: FAIL (compile error — `IsAdminSharedController` doesn't exist yet,
and it isn't `internal`-visible to the test project yet either — see Step 4's
`InternalsVisibleTo` note).

- [ ] **Step 4: Make `Grand.Web.Common`'s internals visible to its test project**

Run: `grep -n "InternalsVisibleTo" src/Web/Grand.Web.Common/Grand.Web.Common.csproj`
to check whether this is already wired. If not, add to
`src/Web/Grand.Web.Common/Grand.Web.Common.csproj`'s first `<ItemGroup>` (or a
new one):
```xml
<ItemGroup>
  <InternalsVisibleTo Include="Grand.Web.Common.Tests" />
</ItemGroup>
```

- [ ] **Step 5: Implement the expander branch**

Replace `src/Web/Grand.Web.Common/View/ViewLocationExpander.cs` in full:

```csharp
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.View;

public class ViewLocationExpander : IViewLocationExpander
{
    private const string ThemeKey = "Theme";
    private const string AdminSharedFallbackLocation = "/Views/{1}/{0}.cshtml";
    private const string AdminSharedControllersNamespace = "Grand.Web.AdminShared.Controllers";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var themeContextFactory =
            context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeContextFactory>();
        var themeContext = themeContextFactory.GetThemeContext(context.AreaName ?? "");
        var themeName = themeContext?.GetCurrentTheme();
        if (!string.IsNullOrEmpty(themeName))
            context.Values[ThemeKey] = themeContext.GetCurrentTheme();
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(ThemeKey, out _))
        {
            var viewFactory = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IViewFactory>();
            viewFactory.GetViewPath(context.AreaName ?? "", ref viewLocations);
        }

        if (IsAdminSharedController(context.ActionContext.ActionDescriptor))
            viewLocations = viewLocations.Append(AdminSharedFallbackLocation);

        return viewLocations;
    }

    /// <summary>Whether the executing action's controller type (or any base type) lives in
    /// Grand.Web.AdminShared.Controllers. Generic by design — no per-entity base-controller
    /// list to maintain: the moment a future Base*Controller (Order, Category, ...) lands in
    /// that namespace, its host subclasses get the AdminShared view fallback automatically.</summary>
    internal static bool IsAdminSharedController(ActionDescriptor descriptor)
    {
        if (descriptor is not ControllerActionDescriptor cad) return false;
        for (var t = cad.ControllerTypeInfo.AsType(); t is not null; t = t.BaseType)
            if (t.Namespace == AdminSharedControllersNamespace)
                return true;
        return false;
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test src/Tests/Grand.Web.Common.Tests --filter "FullyQualifiedName~ViewLocationExpanderTests"`
Expected: PASS (3/3).

- [ ] **Step 7: Build the three hosts**

Run:
```
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj
dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```
Expected: all succeed. The branch is inert until Task 3 gives it a real
`BaseProductController`-derived action and a view to fall back to — this step
only confirms the expander itself compiles and doesn't regress the existing
`ThemeKey` branch (still present, untouched).

- [ ] **Step 8: Commit**

```bash
git add src/Web/Grand.Web.Common/View/ViewLocationExpander.cs src/Web/Grand.Web.Common/Grand.Web.Common.csproj src/Tests/Grand.Web.Common.Tests/View/ViewLocationExpanderTests.cs
git commit -m "Add AdminShared view fallback branch to ViewLocationExpander (ARCH-001 Phase 2)"
```

---

## Task 3: Shared `_ViewImports.cshtml` / `_ViewStart.cshtml` + worked pilot view

This is the template every remaining file in Task 4 follows. Do this one file
fully and correctly before touching any other.

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Views/_ViewImports.cshtml`
- Create: `src/Web/Grand.Web.AdminShared/Views/_ViewStart.cshtml`
- Create: `src/Web/Grand.Web.AdminShared/Views/Product/TierPriceCreatePopup.cshtml`
- Delete: `src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/TierPriceCreatePopup.cshtml`
- Delete: `src/Web/Grand.Web.Store/Areas/Store/Views/Product/TierPriceCreatePopup.cshtml`
- Delete: `src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product/TierPriceCreatePopup.cshtml`

**Interfaces:**
- Consumes: `IAdminDataScope<Product>` (Phase 1, DI-registered per host),
  `LocService`/`IEnumTranslationService` (existing, see each host's own
  `_ViewImports.cshtml`).

- [ ] **Step 1: Create the shared `_ViewImports.cshtml`**

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

- [ ] **Step 2: Create the shared `_ViewStart.cshtml`**

```cshtml
@{
    var area = Context.GetRouteValue("area")?.ToString();
    Layout = $"~/Areas/{area}/Views/Shared/_{area}Layout.cshtml";
}
```

This resolves each host's own layout by name (`_AdminLayout.cshtml`,
`_StoreLayout.cshtml`, `_VendorLayout.cshtml`) using the current request's
area — see spec section 3a for why `_ViewStart.cshtml` must live here rather
than relying on each host's own (it won't be found for a view resolved
through the AdminShared fallback location).

- [ ] **Step 3: Read the three current copies to confirm the diff shape**

Run:
```
diff -u src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/TierPriceCreatePopup.cshtml src/Web/Grand.Web.Store/Areas/Store/Views/Product/TierPriceCreatePopup.cshtml
diff -u src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/TierPriceCreatePopup.cshtml src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product/TierPriceCreatePopup.cshtml
```
Expected (confirmed 2026-08-17): Store's copy differs from Admin's only in
`asp-area="@Constants.AreaStore"` vs `@Constants.AreaAdmin`. Vendor's copy
differs in that plus two `Loc["Vendor.Catalog.Products.TierPrices.AddNew"]`
vs `Loc["Admin.Catalog.Products.TierPrices.AddNew"]` resource-key swaps. No
other content differs across all three — this is a pure "trivial unify" case
(spec section 4, row 1).

- [ ] **Step 4: Write the unified view**

Create `src/Web/Grand.Web.AdminShared/Views/Product/TierPriceCreatePopup.cshtml`:

```cshtml
@model ProductModel.TierPriceModel

@{
    Layout = "";

    //page title
    ViewBag.Title = Loc[$"{Scope.ResourceKeyPrefix}.Catalog.Products.TierPrices.AddNew"];
    var area = ViewContext.RouteData.Values["area"]?.ToString();
}

<form id="TierPriceCreatePopup" asp-area="@area" asp-controller="Product" asp-action="TierPriceCreatePopup"
      asp-route-ProductId="@HtmlExtensions.HtmlEncodeSafe(Context.Request.Query["ProductId"])"
      asp-route-btnId="@HtmlExtensions.HtmlEncodeSafe(Context.Request.Query["btnId"])"
      asp-route-formId="@HtmlExtensions.HtmlEncodeSafe(Context.Request.Query["formId"])">

    <div class="row">
        <div class="col-md-12">
            <div class="x_panel light form-fit">
                <div class="x_title">
                    <div class="caption">
                        <i class="fa fa-cube"></i>
                        @Loc[$"{Scope.ResourceKeyPrefix}.Catalog.Products.TierPrices.AddNew"]
                    </div>
                </div>
                <div class="x_content form">
                    <partial name="Partials/CreateOrUpdateTierPrice" model="Model"/>
                </div>
            </div>
        </div>
    </div>
    <script>
        var mfp = $.magnificPopup.instance;
        $("#TierPriceCreatePopup").submit(function (e) {
            e.preventDefault();
            var form = $(this);
            var url = form.attr('action');
            $.ajax({
                type: "POST",
                url: url,
                data: form.serialize(),
                success: function (data) {
                    if (data == "") {
                        mfp.close();
                    } else {
                        $.magnificPopup.open({
                            items: {
                                src: data,
                                type: 'inline'
                            },
                            callbacks: {
                                open: function () {
                                    $('.mfp-wrap').removeAttr('tabindex');
                                }
                            }
                        });
                    }
                    $('#btnRefreshTierPrices').click();
                }
            });
        });
    </script>
</form>
```

Note `Layout = ""` (a popup with no chrome) — this file happens not to depend
on the new `_ViewStart.cshtml` from Step 2 at all, but every other Task 4 row
that omits `Layout` does, so `_ViewStart.cshtml` must exist before any row
that relies on it is migrated. It's created in this task so it's already in
place for all of Task 4.

- [ ] **Step 5: Delete the three host copies**

```bash
git rm src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/TierPriceCreatePopup.cshtml
git rm src/Web/Grand.Web.Store/Areas/Store/Views/Product/TierPriceCreatePopup.cshtml
git rm src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product/TierPriceCreatePopup.cshtml
```

- [ ] **Step 6: Build all three hosts**

Run:
```
dotnet build src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj
dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```
Expected: all succeed. A missing `@inject` or bad `@using` in the new
`_ViewImports.cshtml`/the view itself shows up here as a Razor compile error
(this project does compile-time Razor validation as part of `dotnet build`,
not only at first request).

- [ ] **Step 7: Manual smoke check**

If a local Kestrel instance is available (per project memory
`reference_running_the_storefront`), open the Admin panel, edit a product,
open its Tier prices tab, click "Add new" — confirm the popup renders with
the correct title, submits, and closes. Repeat once for Store and once for
Vendor (each showing the resource key under their own prefix). If no local
instance is available, note this in the commit message body and rely on the
build success + Task 4/5's aggregate manual pass to catch it.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Migrate TierPriceCreatePopup to AdminShared, pilot for view consolidation (ARCH-001 Phase 2)"
```

---

## Task 4: Migrate the remaining Product views

Same per-row discipline as Phase 1's Task 8/10: one filename per row, each
read across every host that has it, classified, migrated (or left as a
host-specific override), verified with a build, and committed independently.
Follow Task 3's template exactly: same `_ViewImports.cshtml` (already in
place, don't recreate it), same "read all present variants → diff → resolve
`asp-area` literal via `ViewContext.RouteData.Values["area"]` and resource
prefix via `Scope.ResourceKeyPrefix` → write one file in
`Grand.Web.AdminShared/Views/Product/` (or its `Partials/` subfolder, matching
each file's current subfolder) → delete host copies → build → commit" cycle.

**Files (per row):**
- Create: `src/Web/Grand.Web.AdminShared/Views/Product/<name>.cshtml` (or
  `Partials/<name>.cshtml`) — unless the row's classification is "keep as
  host override", in which case no AdminShared file is created.
- Delete: the file's copy in each host that currently has it (2 or 3 of
  `src/Web/Grand.Web.Admin/Areas/Admin/Views/Product/...`,
  `src/Web/Grand.Web.Store/Areas/Store/Views/Product/...`,
  `src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product/...`) — unless kept as
  an override, in which case only the non-kept hosts' copies (if any
  duplicate the kept one byte-for-byte) are candidates for deletion; if in
  doubt, keep all host copies that currently exist for an override row and
  note the ambiguity in the commit message rather than guessing.

**Known baseline (2026-08-17), `diffAS`/`diffAV` = number of changed diff
lines, Admin-vs-Store / Admin-vs-Vendor, `diff -U0` line count; `presence` =
which hosts currently have the file (Vendor is missing two files entirely).
Recompute for any file if the repo has drifted since — this table is a
starting hint, not a substitute for reading the actual current files:**

| # | File | Presence | diffAS | diffAV | Starting hint |
|---|---|---|---|---|---|
| 1 | `AssociateProductToAttributeValuePopup.cshtml` | A,S,V | 18 | 34 | small-medium diff, likely area+prefix only — verify |
| 2 | `AssociatedProductAddPopup.cshtml` | A,S,V | 20 | 34 | same shape as #1 |
| 3 | `AttributeCombinationPopup.cshtml` | A,S,V | 10 | 131 | large Admin/Vendor diff — read closely, may have a real difference |
| 4 | `BulkEdit.cshtml` | A,S,V | 21 | 47 | check for the vendor-scoped bulk-edit grid difference (Phase 1 Task 10 note) |
| 5 | `BundleProductAddPopup.cshtml` | A,S,V | 20 | 30 | same shape as #1 |
| 6 | `Create.cshtml` | A,S,V | 2 | 14 | small — likely trivial unify (relies on the shared `_ViewStart.cshtml` for Layout) |
| 7 | `CrossSellProductAddPopup.cshtml` | A,S,V | 18 | 34 | same shape as #1 |
| 8 | `Edit.cshtml` | A,S,V | 4 | 26 | small-medium — check the Store `EditWarningCheck` hook (Phase 1) has a matching view-side warning banner |
| 9 | `List.cshtml` | A,S,V | 141 | 110 | **real functional difference** — Admin has a bulk export/import/delete panel and Store/Vendor filter panel drops `SearchStoreId`/`SearchVendorId`; likely candidate for host-specific override per host, not a single unified file |
| 10 | `Partials/CreateOrUpdate.Additional.cshtml` | A,S,V | 2 | 94 | Admin/Store trivial; Vendor differs a lot — read before assuming unifiable |
| 11 | `Partials/CreateOrUpdate.AssociatedProducts.cshtml` | A,S,V | 10 | 32 | medium |
| 12 | `Partials/CreateOrUpdate.Bids.cshtml` | A,S,V | 6 | 22 | small-medium |
| 13 | `Partials/CreateOrUpdate.BundleProducts.cshtml` | A,S,V | 10 | 32 | medium |
| 14 | `Partials/CreateOrUpdate.Calendar.cshtml` | A,S,V | 12 | 60 | medium-large |
| 15 | `Partials/CreateOrUpdate.Categories.cshtml` | A,S,V | 12 | 54 | medium-large |
| 16 | `Partials/CreateOrUpdate.Collections.cshtml` | A,S,V | 12 | 44 | medium |
| 17 | `Partials/CreateOrUpdate.CrossSells.cshtml` | A,S,V | 8 | 22 | small-medium |
| 18 | `Partials/CreateOrUpdate.Discounts.cshtml` | A,S only | 0 | n/a | **Admin/Store byte-identical, Vendor has no such tab at all** — unify Admin+Store into one AdminShared file; Vendor simply never requests it (Phase 1's Vendor tab set already excludes Discounts) |
| 19 | `Partials/CreateOrUpdate.Documents.cshtml` | A,S only | 8 | n/a | Admin/Store small diff, Vendor has no such tab — same treatment as #18 once Admin/Store diff is resolved |
| 20 | `Partials/CreateOrUpdate.Info.cshtml` | A,S,V | 26 | 160 | **large diff** — read very closely, likely has real per-host fields (e.g. vendor selector shown/hidden) |
| 21 | `Partials/CreateOrUpdate.Inventory.cshtml` | A,S,V | 0 | 20 | Admin/Store byte-identical; Vendor differs — likely unifiable with a `Scope`-driven conditional |
| 22 | `Partials/CreateOrUpdate.Pictures.cshtml` | A,S,V | 8 | 28 | small-medium |
| 23 | `Partials/CreateOrUpdate.Prices.cshtml` | A,S,V | 8 | 83 | medium-large, Vendor differs a lot |
| 24 | `Partials/CreateOrUpdate.ProductAttributes.TabAttributeCombinations.cshtml` | A,S,V | 12 | 40 | medium |
| 25 | `Partials/CreateOrUpdate.ProductAttributes.TabAttributes.cshtml` | A,S,V | 20 | 42 | medium |
| 26 | `Partials/CreateOrUpdate.ProductAttributes.cshtml` | A,S,V | 0 | 10 | Admin/Store byte-identical |
| 27 | `Partials/CreateOrUpdate.ProductPrices.cshtml` | A,S,V | 8 | 49 | medium |
| 28 | `Partials/CreateOrUpdate.PurchasedWithOrders.cshtml` | A,S,V | 6 | 52 | medium |
| 29 | `Partials/CreateOrUpdate.Recommended.cshtml` | A,S,V | 8 | 22 | small-medium |
| 30 | `Partials/CreateOrUpdate.RelatedProducts.cshtml` | A,S,V | 10 | 26 | small-medium |
| 31 | `Partials/CreateOrUpdate.Reviews.cshtml` | A,S,V | 6 | 20 | small-medium |
| 32 | `Partials/CreateOrUpdate.SEO.cshtml` | A,S,V | 0 | 4 | tiny — near-trivial |
| 33 | `Partials/CreateOrUpdate.SimilarProducts.cshtml` | A,S,V | 10 | 26 | small-medium |
| 34 | `Partials/CreateOrUpdate.SpecificationAttributes.cshtml` | A,S,V | 10 | 26 | small-medium |
| 35 | `Partials/CreateOrUpdate.cshtml` | A,S,V | 0 | 60 | Admin/Store byte-identical; Vendor differs (likely the tab list itself — fewer tabs for Vendor, e.g. no Discounts/Documents) — read closely, this is the tab-container partial |
| 36 | `Partials/CreateOrUpdateProductAttributeValue.cshtml` | A,S,V | 2 | 10 | small — likely trivial |
| 37 | `Partials/CreateOrUpdateTierPrice.cshtml` | A,S,V | 0 | 22 | Admin/Store byte-identical; Vendor differs |
| 38 | `Partials/ProductAttributes.cshtml` | A,S,V | 0 | 0 | **byte-identical across all three hosts already** — trivial unify, no conditionals needed |
| 39 | `ProductAttributeConditionPopup.cshtml` | A,S,V | 2 | 16 | small |
| 40 | `ProductAttributeMappingPopup.cshtml` | A,S,V | 2 | 10 | small — same shape as Task 3's pilot |
| 41 | `ProductAttributeValidationRulesPopup.cshtml` | A,S,V | 2 | 8 | small — same shape as Task 3's pilot |
| 42 | `ProductAttributeValueCreatePopup.cshtml` | A,S,V | 2 | 6 | small — same shape as Task 3's pilot, diff confirmed 2026-08-17 |
| 43 | `ProductAttributeValueEditPopup.cshtml` | A,S,V | 2 | 6 | small — same shape as Task 3's pilot |
| 44 | `ProductPicturePopup.cshtml` | A,S,V | 2 | 8 | small |
| 45 | `ProductSpecAttrPopup.cshtml` | A,S,V | 4 | 10 | small |
| 46 | `RecommendedProductAddPopup.cshtml` | A,S,V | 20 | 34 | same shape as #1 |
| 47 | `RelatedProductAddPopup.cshtml` | A,S,V | 18 | 32 | same shape as #1 |
| 48 | `RequiredProductAddPopup.cshtml` | A,S,V | 18 | 52 | medium |
| 49 | `SimilarProductAddPopup.cshtml` | A,S,V | 20 | 32 | same shape as #1 |
| 50 | `TierPriceEditPopup.cshtml` | A,S,V | 2 | 6 | small — same shape as Task 3's pilot |

(`TierPriceCreatePopup.cshtml` is row 0, done in Task 3.)

- [ ] **Step 1 (repeat per row): read, classify, migrate (or override), build, commit**

For each row:
1. Read every present host's copy of the file in full.
2. Classify per spec section 4:
   - Byte-identical or differs only by area literal / resource prefix already
     covered by `Scope.ResourceKeyPrefix` → write one file in AdminShared,
     following Task 3's pattern (`ViewContext.RouteData.Values["area"]` for
     the area, `Scope.ResourceKeyPrefix` for resource keys), delete host
     copies.
   - Differs by a capability flag Phase 1 already introduced on the view
     model (e.g. a bool controlling whether a field renders) → one file with
     an `@if (Model.SomeFlag) { ... }`, delete host copies.
   - Real functional difference → leave every existing host copy exactly
     where it is; do not touch it, do not create an AdminShared file for it.
     `RazorViewEngine`'s host-location-first ordering (Task 2) means these
     continue rendering exactly as before with zero code change — this row
     is only a documented "leave alone" decision, still worth its own commit
     noting why, for the audit trail.
3. Build: `dotnet build src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj && dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj && dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj && dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj`
4. Commit:
```bash
git add -A
git commit -m "Migrate <FileName> to AdminShared (ARCH-001 Phase 2)"
# or, for a "leave alone" row:
git commit -m "Keep <FileName> as host-specific override, no unification (ARCH-001 Phase 2)" --allow-empty
```

- [ ] **Step 2: After all 50 rows are checked off, confirm no orphaned host copies remain for unified files**

Run:
```
find src/Web/Grand.Web.Admin/Areas/Admin/Views/Product src/Web/Grand.Web.Store/Areas/Store/Views/Product src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product -iname "*.cshtml" | sort
find src/Web/Grand.Web.AdminShared/Views/Product -iname "*.cshtml" | sort
```
Cross-check against the row-by-row decisions recorded in commit messages —
every file that was unified should no longer exist under any host's own
`Views/Product/`, and every file kept as an override should exist in exactly
the hosts that had it originally (not fewer, not more).

---

## Task 5: Full-solution verification

**Files:** none — verification only.

- [ ] **Step 1: Full solution build**

Run: `dotnet build GrandNode.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Full test run for the affected test projects**

Run (individually, per project memory `project_test_suite_flaky_parallel` —
not a single solution-wide `dotnet test`):
```
dotnet test src/Tests/Grand.Web.Common.Tests
dotnet test src/Tests/Grand.Web.Admin.Tests
dotnet test src/Tests/Grand.Web.Store.Tests
dotnet test src/Tests/Grand.Web.Vendor.Tests
```
Expected: all PASS.

- [ ] **Step 3: File-count sanity check against the ARCH-001 Phase 2 baseline**

Run:
```
find src/Web/Grand.Web.Admin/Areas/Admin/Views/Product -iname "*.cshtml" | wc -l
find src/Web/Grand.Web.Store/Areas/Store/Views/Product -iname "*.cshtml" | wc -l
find src/Web/Grand.Web.Vendor/Areas/Vendor/Views/Product -iname "*.cshtml" | wc -l
find src/Web/Grand.Web.AdminShared/Views/Product -iname "*.cshtml" | wc -l
```
Expected: the three host counts have dropped from the Task 0 baseline
(51/51/49) by however many rows were unified in Task 4; `AdminShared/Views/
Product` holds that many files (plus 1 for Task 3's pilot). The three host
counts plus the AdminShared count, accounting for files present in more than
one host before migration, should reconcile against Task 4's per-row log —
if they don't, some row was migrated inconsistently; find and fix it before
proceeding.

- [ ] **Step 4: Manual smoke test**

If a local Kestrel instance is available (per project memory
`reference_running_the_storefront`): log into each of the three admin panels
and open Product → List → Create → Edit → Save for one existing product per
host. Confirm:
- No `InvalidOperationException: The view '...' was not found` error for any
  action.
- Each host's layout/chrome renders correctly (validates the `_ViewStart.cshtml`
  fix from Task 3/spec section 3a).
- Host-specific content still appears only where expected (Admin's bulk
  export/import/delete panel on `List.cshtml`; Vendor has no
  Discounts/Documents tab).
- Popups (tier price, product attribute value, etc.) open, submit, and close
  correctly on all three hosts.

If no local instance is available, report this explicitly as unverified
rather than claiming the pass was done.

- [ ] **Step 5: Update the ARCH-001 project memory**

Edit the memory file `project_arch001_triple_admin_duplication.md` (outside
this repo, in the memory directory) to record Phase 2 complete: views
unified, host-specific overrides count, any views deliberately left
un-unified and why, and that the `Grand.Web.AdminShared` + `ViewLocationExpander`
pattern is now proven end-to-end (controller, service, and view layers) and
ready to reuse for the next entity.

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "ARCH-001 Phase 2 complete: Product views consolidated into AdminShared"
```
