# New Product Badge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Display a "New" badge on the product details page for products whose `CreatedOnUtc >= DateTime.UtcNow.AddDays(-30)`, using a new `ShowNewBadge` property that does not touch the existing `Flag` field.

**Architecture:** Add computed `bool ShowNewBadge` to `ProductDetailsModel`. Set it in `GetProductDetailsPageHandler.PrepareStandardProperties()` alongside the existing `Flag` assignment. Render it conditionally in both the core view and the Theme.Modern override. No service changes, no domain entity changes, no cache key changes.

**Tech Stack:** C# / ASP.NET Core, Razor Views, MSTest, Moq

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| MODIFY | `src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs` | Add `bool ShowNewBadge { get; set; }` |
| MODIFY | `src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs` | Populate `ShowNewBadge` in `PrepareStandardProperties()` |
| MODIFY | `src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml` | Render badge block (core view) |
| MODIFY | `src/Plugins/Theme.Modern/Views/Modern/Product/ProductLayout.Simple.cshtml` | Render badge block (theme override) |
| CREATE | `src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs` | Unit tests for badge date logic |

**Out of scope:** `ProductLayout.Grouped.cshtml` (neither core nor theme version), admin UI, domain entity, any service layer, cache invalidation.

---

## Context: Key Facts

- `ProductDetailsModel.Flag` (line 27) is admin-managed free-text. **Do not write to it.**
- `GetProductDetailsPageHandler.PrepareStandardProperties()` (~line 345) builds `ProductDetailsModel`. The line `Flag = product.Flag,` (~line 360) is the insertion point for `ShowNewBadge`.
- Core view already renders `Model.Flag` as a badge block at lines 64–69. `ShowNewBadge` goes immediately after it.
- **Theme.Modern** has its own `ProductLayout.Simple.cshtml` at `src/Plugins/Theme.Modern/Views/Modern/Product/ProductLayout.Simple.cshtml`. It does **not** have the Flag block. Both Flag and ShowNewBadge blocks must be added there.
- The handler uses `ICacheBase` for sub-components (collections, etc.) but the main model property block is not independently cached. No cache key changes are needed.

---

## Task 1: Add `ShowNewBadge` to `ProductDetailsModel` (TDD)

**Files:**
- Create: `src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs`
- Modify: `src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs`

- [x] **Step 1: Write the failing test**

Create `src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs`:

```csharp
using Grand.Web.Models.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests;

[TestClass]
public class ProductNewBadgeTests
{
    [TestMethod]
    public void ProductDetailsModel_HasShowNewBadgeProperty()
    {
        var model = new ProductDetailsModel();
        model.ShowNewBadge = true;
        Assert.IsTrue(model.ShowNewBadge);
    }
}
```

- [x] **Step 2: Run test to confirm it fails**

```
dotnet test src/Tests/Grand.Web.Common.Tests/Grand.Web.Common.Tests.csproj --filter "ProductNewBadgeTests" -v minimal
```

Expected: compile error — `'ProductDetailsModel' does not contain a definition for 'ShowNewBadge'`

- [x] **Step 3: Add the property to `ProductDetailsModel`**

In `src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs`, add one line after `public string Flag { get; set; }` (line 27):

```csharp
public string Flag { get; set; }
public bool ShowNewBadge { get; set; }
```

- [x] **Step 4: Run test to confirm it passes**

```
dotnet test src/Tests/Grand.Web.Common.Tests/Grand.Web.Common.Tests.csproj --filter "ProductNewBadgeTests" -v minimal
```

Expected: 1 test PASSED

- [x] **Step 5: Commit**

```bash
git add src/Web/Grand.Web/Models/Catalog/ProductDetailsModel.cs \
        src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs
git commit -m "feat: add ShowNewBadge property to ProductDetailsModel"
```

---

## Task 2: Add badge date-logic tests and populate `ShowNewBadge` in the handler

**Files:**
- Modify: `src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs`
- Modify: `src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs`

- [x] **Step 1: Add the three date-logic tests**

Append these three test methods inside the `ProductNewBadgeTests` class:

```csharp
[TestMethod]
public void BadgeLogic_ProductCreatedToday_IsTrue()
{
    var createdOnUtc = DateTime.UtcNow;
    var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
    Assert.IsTrue(result);
}

[TestMethod]
public void BadgeLogic_ProductCreatedWithin30Days_IsTrue()
{
    var createdOnUtc = DateTime.UtcNow.AddDays(-29);
    var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
    Assert.IsTrue(result);
}

[TestMethod]
public void BadgeLogic_ProductCreated31DaysAgo_IsFalse()
{
    var createdOnUtc = DateTime.UtcNow.AddDays(-31);
    var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
    Assert.IsFalse(result);
}
```

- [x] **Step 2: Run tests to confirm they pass**

```
dotnet test src/Tests/Grand.Web.Common.Tests/Grand.Web.Common.Tests.csproj --filter "ProductNewBadgeTests" -v minimal
```

Expected: 4 tests PASSED

- [x] **Step 3: Populate `ShowNewBadge` in the handler**

In `src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs`, locate `PrepareStandardProperties()`. Find the object initializer line:

```csharp
Flag = product.Flag,
```

Add `ShowNewBadge` on the next line inside the same initializer block:

```csharp
Flag = product.Flag,
ShowNewBadge = product.CreatedOnUtc >= DateTime.UtcNow.AddDays(-30),
```

- [x] **Step 4: Build to confirm no errors**

```
dotnet build GrandNode.sln --no-restore
```

Expected: Build succeeded, 0 errors

- [x] **Step 5: Commit**

```bash
git add src/Web/Grand.Web/Features/Handlers/Products/GetProductDetailsPageHandler.cs \
        src/Tests/Grand.Web.Common.Tests/ProductNewBadgeTests.cs
git commit -m "feat: populate ShowNewBadge from product.CreatedOnUtc in GetProductDetailsPageHandler"
```

---

## Task 3: Render the badge in the core product layout view

**Files:**
- Modify: `src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml`

- [x] **Step 1: Locate the Flag block**

Open `src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml`. The existing Flag block is around line 64:

```html
@if (!string.IsNullOrEmpty(Model.Flag))
{
    <div class="product-label">
        <div class="badge badge-info">@Model.Flag</div>
    </div>
}
```

- [x] **Step 2: Add the ShowNewBadge block immediately after the Flag block**

```html
@if (!string.IsNullOrEmpty(Model.Flag))
{
    <div class="product-label">
        <div class="badge badge-info">@Model.Flag</div>
    </div>
}
@if (Model.ShowNewBadge)
{
    <div class="product-label">
        <div class="badge badge-success">New</div>
    </div>
}
```

- [x] **Step 3: Build to confirm no errors**

```
dotnet build GrandNode.sln --no-restore
```

Expected: Build succeeded, 0 errors

- [x] **Step 4: Commit**

```bash
git add src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml
git commit -m "feat: render ShowNewBadge in core ProductLayout.Simple view"
```

---

## Task 4: Render the badge in the Theme.Modern product layout view

**Files:**
- Modify: `src/Plugins/Theme.Modern/Views/Modern/Product/ProductLayout.Simple.cshtml`

**Important:** Theme.Modern's `ProductLayout.Simple.cshtml` does **not** contain the Flag rendering block that the core view has. Both Flag and ShowNewBadge blocks must be added, so the theme is consistent with the core.

- [x] **Step 1: Locate the insertion point**

Open `src/Plugins/Theme.Modern/Views/Modern/Product/ProductLayout.Simple.cshtml`. Find the overview column, approximately line 63:

```html
<b-col xl="5" lg="6" md="7" cols="12" class="overview pl-md-3 pl-0 pr-0">
    <partial name="Partials/Unavailable" model="Model"/>
```

- [x] **Step 2: Add both badge blocks inside the overview column, before the Unavailable partial**

```html
<b-col xl="5" lg="6" md="7" cols="12" class="overview pl-md-3 pl-0 pr-0">
    @if (!string.IsNullOrEmpty(Model.Flag))
    {
        <div class="product-label">
            <div class="badge badge-info">@Model.Flag</div>
        </div>
    }
    @if (Model.ShowNewBadge)
    {
        <div class="product-label">
            <div class="badge badge-success">New</div>
        </div>
    }
    <partial name="Partials/Unavailable" model="Model"/>
```

- [x] **Step 3: Build the full solution**

```
dotnet build GrandNode.sln --no-restore
```

Expected: Build succeeded, 0 errors

- [x] **Step 4: Run all tests**

```
dotnet test GrandNode.sln --no-restore
```

Expected: All tests pass

- [x] **Step 5: Commit**

```bash
git add src/Plugins/Theme.Modern/Views/Modern/Product/ProductLayout.Simple.cshtml
git commit -m "feat: render Flag and ShowNewBadge badges in Theme.Modern ProductLayout.Simple view"
```

---

## Spec Coverage Check

| Requirement | Covered by |
|-------------|-----------|
| Product created within 30 days shows "New" badge | Task 2 (logic), Tasks 3 & 4 (render) |
| Product older than 30 days does not show the badge | Task 2 tests (`BadgeLogic_ProductCreated31DaysAgo_IsFalse`) |
| Badge visible on product details page | Tasks 3 & 4 — both core and theme views |
| Existing product details behavior not broken | `Flag` is untouched; badge is a separate conditional block |
| Follows existing project patterns | Badge HTML matches existing `Flag` block structure exactly |
| Tests added where practical | Tasks 1 & 2 — 4 unit tests covering boundary and happy paths |

## Out-of-Scope Notes

- `ProductLayout.Grouped.cshtml` exists in both core and Theme.Modern but is not targeted by this feature spec. Apply the same Razor changes to those files if grouped-product pages are expected to show the badge.
- No cache key changes are needed. `ShowNewBadge` is set in the non-cached property block of `PrepareStandardProperties()`.
- No admin UI changes. The badge is entirely computed from `CreatedOnUtc` and is not configurable.
