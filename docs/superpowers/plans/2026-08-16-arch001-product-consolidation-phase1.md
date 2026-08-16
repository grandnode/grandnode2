# ARCH-001 Product Consolidation — Phase 1 (Controller + Service) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Tasks 8 and 10 are per-item checklists (one region / one method each) — when using subagent-driven-development, dispatch one subagent per checklist row, not one subagent for the whole task.

**Goal:** Eliminate the three duplicated `ProductController` classes (Admin/Store/Vendor) and the two duplicated `ProductViewModelService` classes (AdminShared/Vendor) by consolidating the logic into `Grand.Web.AdminShared`, scoped per host through a new `IAdminDataScope<TEntity>` strategy, with each host reduced to a thin subclass — mirroring the existing `BaseLoginController` pattern.

**Architecture:** A new `IAdminDataScope<TEntity>` (data filtering/access/default-value strategy) and a `ResourceKeyPrefix` (host-specific localization key prefix) are injected into a single `BaseProductController` and a single `ProductViewModelService`, both living in `Grand.Web.AdminShared`. Each host (`Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`) registers its own `IAdminDataScope<Product>` implementation and keeps only a ~20-line `ProductController : BaseProductController` subclass declaring `[Area(...)]`.

**Tech Stack:** ASP.NET Core MVC, C# 13 primary constructors, MSTest + Moq (existing test stack per `.ai/knowledge/tests.md`), MongoDB repositories (unaffected — no data-layer changes in this plan).

**Spec:** `docs/superpowers/specs/2026-08-16-arch001-product-consolidation-design.md`

## Global Constraints

- Do not merge the three hosts into one deployable app — separate auth models, data scopes, and independent deployability stay separate (spec, "Out of scope").
- Direct migration, no feature flag / parallel-run — chosen deliberately because characterization tests already exist for all three `ProductController`s and both `ProductViewModelService`s (see Task 0).
- `IAdminDataScope<TEntity>` ships only what `Product` needs (`HasAccess`, `ApplyScope`, `DefaultStoreId`) — no speculative members for entities not yet migrated.
- Every host-specific literal (StaffStoreId access, `HasAccessToProduct`, `"Admin."`/`"Vendor."` resource-key prefixes) must route through `IAdminDataScope<Product>` or `ResourceKeyPrefix` — no residual host-specific `if` branches left inside `BaseProductController` or the shared service.
- This plan is Phase 1 only (controller + service). Views (Phase 2) are a separate, later plan per the spec.
- Follow existing repo conventions: primary-constructor DI (see `95c8548bf`), `.ai/standards/csharp-style.md`, `.ai/knowledge/mongodb.md` for any repository-layer touches (none expected in this plan).

---

## Task 0: Baseline — confirm the safety net before touching anything

**Files:**
- Read only: `src/Tests/Grand.Web.Admin.Tests/Controllers/ProductControllerTests.cs`
- Read only: `src/Tests/Grand.Web.Store.Tests/Controllers/ProductControllerTests.cs`
- Read only: `src/Tests/Grand.Web.Vendor.Tests/Controllers/ProductControllerTests.cs`
- Read only: `src/Tests/Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs`
- Read only: `src/Tests/Grand.Web.Vendor.Tests/Services/ProductViewModelServiceTests.cs`

**Interfaces:** none (read-only verification task).

- [ ] **Step 1: Run all five existing test files and confirm they currently pass**

Run:
```
dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~ProductController|FullyQualifiedName~ProductViewModelService"
dotnet test src/Tests/Grand.Web.Store.Tests
dotnet test src/Tests/Grand.Web.Vendor.Tests --filter "FullyQualifiedName~ProductController|FullyQualifiedName~ProductViewModelService"
```
Note: run `Grand.Web.Store.Tests` **unfiltered**, not with a `~ProductController` filter. `AutoMapperConfig` is a static singleton initialized in `PaymentControllerTests.TestInitialize` (`src/Tests/Grand.Web.Store.Tests/Controllers/PaymentControllerTests.cs:48`); filtering it out of the run leaves the mapper uninitialized and fails `ProductControllerTests.EditGet_ProductSharedAcrossMultipleStoresIncludingStaffStore_ShowsFormWithWarning` with a `NullReferenceException` that has nothing to do with Product code (confirmed 2026-08-16: 102/102 pass unfiltered, 93/94 pass with the narrow filter). Same applies anywhere else in this plan that filters `Grand.Web.Store.Tests` by `~Product*` — run that project unfiltered instead.

Expected: all PASS. If anything fails here, stop and fix or report it before starting Task 1 — this plan's safety net depends on a green baseline.

- [ ] **Step 2: Note the test project namespaces/base classes used**

Skim each file's `using` block and test class setup (constructor mocks) — later tasks reuse these mock-setup patterns when writing new `IAdminDataScope` tests. No code change in this step.

---

## Task 1: `IAdminDataScope<TEntity>` interface

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Interfaces/IAdminDataScope.cs`

**Interfaces:**
- Produces: `IAdminDataScope<TEntity>` with `Task<bool> HasAccess(TEntity entity)`, `Task<bool> CanView(TEntity entity)` (default interface method, defaults to `HasAccess`), `IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query)`, `string? DefaultStoreId { get; }`, `string ResourceKeyPrefix { get; }` — consumed by Tasks 2-4 (implementations) and Task 7+ (`BaseProductController`)/Task 9+ (shared service).

**Addendum (added after Task 7's review found a real gap — see Task 7's fix round):** `CanView` was added retroactively as a default interface method, so this is additive/source-compatible — `GlobalAdminDataScope` and `VendorProductDataScope` (Tasks 2 and 4) need no change, only `StoreAdminDataScope` (Task 3) gets an override plus a test.

- [ ] **Step 1: Write the interface**

```csharp
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
    /// `CheckAccessToProduct`). Only Store overrides this (see Task 3 addendum).</summary>
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
```

- [ ] **Step 2: Build to confirm it compiles**

Run: `dotnet build src/Web/Grand.Web.AdminShared/Grand.Web.AdminShared.csproj`
Expected: Build succeeded (interface has no consumers yet, so nothing else changes).

- [ ] **Step 3: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Interfaces/IAdminDataScope.cs
git commit -m "Add IAdminDataScope<TEntity> abstraction (ARCH-001 Phase 1)"
```

---

## Task 2: `GlobalAdminDataScope<TEntity>` (Admin host)

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Services/GlobalAdminDataScope.cs`
- Test: `src/Tests/Grand.Web.Admin.Tests/Services/GlobalAdminDataScopeTests.cs`

**Interfaces:**
- Consumes: `IAdminDataScope<TEntity>` (Task 1).
- Produces: `GlobalAdminDataScope<TEntity> : IAdminDataScope<TEntity>` — registered by Admin's `Startup` in Task 5.

- [ ] **Step 1: Write the failing test**

```csharp
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class GlobalAdminDataScopeTests
{
    [TestMethod]
    public async Task HasAccess_AlwaysReturnsTrue()
    {
        var scope = new GlobalAdminDataScope<Product>();
        var result = await scope.HasAccess(new Product());
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ApplyScope_ReturnsQueryUnchanged()
    {
        var scope = new GlobalAdminDataScope<Product>();
        var query = new[] { new Product { Id = "1" }, new Product { Id = "2" } }.AsQueryable();

        var result = scope.ApplyScope(query);

        CollectionAssert.AreEqual(query.ToList(), result.ToList());
    }

    [TestMethod]
    public void DefaultStoreId_IsNull()
    {
        var scope = new GlobalAdminDataScope<Product>();
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsAdmin()
    {
        var scope = new GlobalAdminDataScope<Product>();
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~GlobalAdminDataScopeTests"`
Expected: FAIL (compile error — `GlobalAdminDataScope` does not exist yet).

- [ ] **Step 3: Write the implementation**

```csharp
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class GlobalAdminDataScope<TEntity> : IAdminDataScope<TEntity>
{
    public Task<bool> HasAccess(TEntity entity) => Task.FromResult(true);

    public IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query) => query;

    public string? DefaultStoreId => null;

    public string ResourceKeyPrefix => "Admin";
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~GlobalAdminDataScopeTests"`
Expected: PASS (4/4).

- [ ] **Step 5: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Services/GlobalAdminDataScope.cs src/Tests/Grand.Web.Admin.Tests/Services/GlobalAdminDataScopeTests.cs
git commit -m "Add GlobalAdminDataScope for the Admin host (ARCH-001 Phase 1)"
```

---

## Task 3: `StoreAdminDataScope<TEntity>` (Store host)

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Services/StoreAdminDataScope.cs`
- Test: `src/Tests/Grand.Web.Store.Tests/Services/StoreAdminDataScopeTests.cs`

**Interfaces:**
- Consumes: `IAdminDataScope<TEntity>` (Task 1), existing `IContextAccessor`/`IWorkContext` (`Grand.Infrastructure`), existing `IStoreLinkEntity` (`Grand.Domain.Stores` — already implemented by `Product`, confirmed at `src/Core/Grand.Domain/Catalog/Product.cs:754`).
- Produces: `StoreAdminDataScope<TEntity> where TEntity : IStoreLinkEntity` — registered by Store's `Startup` in Task 5.

- [ ] **Step 1: Write the failing tests**

```csharp
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Services;

[TestClass]
public class StoreAdminDataScopeTests
{
    private Mock<IContextAccessor> _contextAccessor = null!;
    private const string StaffStoreId = "store-1";

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
    }

    // These three mirror AclMappingExtension.AccessToEntityByStore's existing, deliberately strict
    // rule (src/Web/Grand.Web.AdminShared/Extensions/AclMappingExtension.cs), the same rule
    // ProductController.CanAccessProduct already enforces for Edit(POST)/Delete/CopyProduct today
    // (see src/Tests/Grand.Web.Store.Tests/Controllers/ProductControllerTests.cs,
    // Delete_ProductNotLimitedToAnyStore_IsDenied and
    // Delete_ProductInMultipleStoresIncludingStaffStore_IsDenied, both commented
    // "counter-intuitive but current behavior... must not silently fix this"). Access is granted
    // ONLY when the product is limited to stores, is in exactly one store, and that store is the
    // staff member's store — a global product or one shared across multiple stores is denied.
    [TestMethod]
    public async Task HasAccess_ProductNotLimitedToStores_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = false };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToOtherStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = ["store-2"] };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductLimitedToStaffStoreOnly_ReturnsTrue()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId] };

        Assert.IsTrue(await scope.HasAccess(product));
    }

    [TestMethod]
    public async Task HasAccess_ProductInMultipleStoresIncludingStaffStore_ReturnsFalse()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        var product = new Product { LimitedToStores = true, Stores = [StaffStoreId, "store-3"] };

        Assert.IsFalse(await scope.HasAccess(product));
    }

    [TestMethod]
    public void DefaultStoreId_ReturnsStaffStoreId()
    {
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.AreEqual(StaffStoreId, scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsAdmin()
    {
        // Store has no distinct resource set for Product screens yet (see Task 6) — it renders
        // AdminShared's "Admin.*" keys today, so the migrated scope must keep that behavior.
        var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
        Assert.AreEqual("Admin", scope.ResourceKeyPrefix);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test src/Tests/Grand.Web.Store.Tests --filter "FullyQualifiedName~StoreAdminDataScopeTests"`
Expected: FAIL (compile error — type doesn't exist).

- [ ] **Step 3: Write the implementation — delegate to the existing `AccessToEntityByStore` extension**

Do not reimplement the store-access rule. `src/Web/Grand.Web.AdminShared/Extensions/AclMappingExtension.cs` already has it (`AccessToEntityByStore<T>(this T entity, string storeId) where T : BaseEntity, IStoreLinkEntity`), and it's the same rule `ProductController.CanAccessProduct` already enforces today. Add the `BaseEntity` constraint and call it directly:

```csharp
using Grand.Domain;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class StoreAdminDataScope<TEntity>(IContextAccessor contextAccessor) : IAdminDataScope<TEntity>
    where TEntity : BaseEntity, IStoreLinkEntity
{
    public Task<bool> HasAccess(TEntity entity)
    {
        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        return Task.FromResult(entity != null && entity.AccessToEntityByStore(staffStoreId));
    }

    public IQueryable<TEntity> ApplyScope(IQueryable<TEntity> query)
    {
        var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
        if (string.IsNullOrEmpty(staffStoreId)) return query;
        return query.Where(x => x.LimitedToStores && x.Stores.Contains(staffStoreId) && x.Stores.Count == 1);
    }

    public string? DefaultStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public string ResourceKeyPrefix => "Admin";
}
```

`ApplyScope` mirrors the same strict rule inline (there's no queryable-friendly overload of `AccessToEntityByStore` — it's written for a single loaded entity) so that a product list built through this scope shows exactly the products `HasAccess` would allow, keeping the two methods consistent with each other.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/Tests/Grand.Web.Store.Tests --filter "FullyQualifiedName~StoreAdminDataScopeTests"`
Expected: PASS (6/6).

- [ ] **Step 5: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Services/StoreAdminDataScope.cs src/Tests/Grand.Web.Store.Tests/Services/StoreAdminDataScopeTests.cs
git commit -m "Add StoreAdminDataScope for the Store host (ARCH-001 Phase 1)"
```

**Note for Task 7/8:** the current `Grand.Web.Store/Controllers/ProductController.cs:88-92` `CanAccessProduct` helper (added in #786, uses `product.AccessToEntityByStore(staffStoreId)`) and this `HasAccess` implementation must agree. `AccessToEntityByStore` is the existing extension in `Grand.Business.Core.Extensions`; check its exact semantics against the `HasAccess` body above during Task 7 Step 1 and use whichever is authoritative (prefer calling the existing `AccessToEntityByStore` extension from inside `HasAccess` over reimplementing the same rule twice, if its signature fits `IStoreLinkEntity`).

**Addendum — `CanView` override (added after Task 7's review found this task's `HasAccess` alone can't represent Store's actual behavior):**

`Grand.Web.Store/Controllers/ProductController.cs:184-194`'s `Edit(GET)` is NOT `HasAccess` plus a cosmetic warning — it is a materially looser rule that *replaces* the strict check for viewing: a global product or a multi-store product that includes the staff member's store is **allowed to view** (with a warning), and only a product limited to stores that exclude the staff member's store is denied. The existing test `Grand.Web.Store.Tests/Controllers/ProductControllerTests.cs:248-264` (`EditGet_ProductSharedAcrossMultipleStoresIncludingStaffStore_ShowsFormWithWarning`) locks this in with an explicit comment: *"This is the one path that must stay outside any shared 'authorize or redirect' helper."* `Store/ProductController.cs:289-290`'s `CopyProduct` uses the same looser rule (denies only when `LimitedToStores && !Stores.Contains(staff)`).

Add to `StoreAdminDataScope<TEntity>` (Task 3's file), after `HasAccess`:

```csharp
public Task<bool> CanView(TEntity entity)
{
    if (entity is null) return Task.FromResult(false);
    var staffStoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
    var allowed = !entity.LimitedToStores || entity.Stores.Contains(staffStoreId);
    return Task.FromResult(allowed);
}
```

Add tests to `StoreAdminDataScopeTests.cs`:

```csharp
[TestMethod]
public async Task CanView_ProductNotLimitedToStores_ReturnsTrue()
{
    var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
    Assert.IsTrue(await scope.CanView(new Product { LimitedToStores = false }));
}

[TestMethod]
public async Task CanView_ProductInMultipleStoresIncludingStaffStore_ReturnsTrue()
{
    var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
    var product = new Product { LimitedToStores = true, Stores = [StaffStoreId, "store-3"] };
    Assert.IsTrue(await scope.CanView(product));
}

[TestMethod]
public async Task CanView_ProductLimitedToOtherStore_ReturnsFalse()
{
    var scope = new StoreAdminDataScope<Product>(_contextAccessor.Object);
    var product = new Product { LimitedToStores = true, Stores = ["store-2"] };
    Assert.IsFalse(await scope.CanView(product));
}
```

Run `dotnet test src/Tests/Grand.Web.Store.Tests --filter "FullyQualifiedName~StoreAdminDataScopeTests"` — expect 9/9 passing. Commit alongside (or amend into) Task 3's existing commit is fine since this is a direct addendum to the same file/task, not a new task.

---

## Task 4: `VendorProductDataScope` (Vendor host, Product-specific)

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Services/VendorProductDataScope.cs`
- Test: `src/Tests/Grand.Web.Vendor.Tests/Services/VendorProductDataScopeTests.cs`

**Interfaces:**
- Consumes: `IAdminDataScope<Product>` (Task 1), existing `IContextAccessor`/`IWorkContext.CurrentVendor`.
- Produces: `VendorProductDataScope : IAdminDataScope<Product>` — registered by Vendor's `Startup` in Task 5.

Named `VendorProductDataScope` (not generic `VendorAdminDataScope<TEntity>`): vendor ownership is keyed by `VendorId` on `Product`, but other entities (`Order`, `Shipment`, ...) use different owner fields (see `src/Web/Grand.Web.Vendor/Extensions/HasAccess.cs`), so a generic vendor scope would need a marker interface that doesn't exist yet. Out of scope for this plan — future entities get their own `Vendor<Entity>DataScope` when migrated, following this same shape.

- [ ] **Step 1: Write the failing tests**

```csharp
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Vendor.Tests.Services;

[TestClass]
public class VendorProductDataScopeTests
{
    private Mock<IContextAccessor> _contextAccessor = null!;
    private const string VendorId = "vendor-1";

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentVendor).Returns(new Domain.Vendors.Vendor { Id = VendorId });
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
    }

    [TestMethod]
    public async Task HasAccess_OwnProduct_ReturnsTrue()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsTrue(await scope.HasAccess(new Product { VendorId = VendorId }));
    }

    [TestMethod]
    public async Task HasAccess_OtherVendorsProduct_ReturnsFalse()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsFalse(await scope.HasAccess(new Product { VendorId = "vendor-2" }));
    }

    [TestMethod]
    public async Task HasAccess_NullProduct_ReturnsFalse()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsFalse(await scope.HasAccess(null!));
    }

    [TestMethod]
    public void ApplyScope_FiltersToOwnVendorId()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        var query = new[]
        {
            new Product { Id = "1", VendorId = VendorId },
            new Product { Id = "2", VendorId = "vendor-2" }
        }.AsQueryable();

        var result = scope.ApplyScope(query).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("1", result[0].Id);
    }

    [TestMethod]
    public void DefaultStoreId_IsNull()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.IsNull(scope.DefaultStoreId);
    }

    [TestMethod]
    public void ResourceKeyPrefix_IsVendor()
    {
        var scope = new VendorProductDataScope(_contextAccessor.Object);
        Assert.AreEqual("Vendor", scope.ResourceKeyPrefix);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test src/Tests/Grand.Web.Vendor.Tests --filter "FullyQualifiedName~VendorProductDataScopeTests"`
Expected: FAIL (compile error).

- [ ] **Step 3: Write the implementation**

```csharp
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class VendorProductDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Product>
{
    public Task<bool> HasAccess(Product entity)
    {
        if (entity is null) return Task.FromResult(false);
        return Task.FromResult(entity.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);
    }

    public IQueryable<Product> ApplyScope(IQueryable<Product> query)
    {
        var vendorId = contextAccessor.WorkContext.CurrentVendor.Id;
        return query.Where(x => x.VendorId == vendorId);
    }

    public string? DefaultStoreId => null;

    public string ResourceKeyPrefix => "Vendor";
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/Tests/Grand.Web.Vendor.Tests --filter "FullyQualifiedName~VendorProductDataScopeTests"`
Expected: PASS (6/6).

- [ ] **Step 5: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Services/VendorProductDataScope.cs src/Tests/Grand.Web.Vendor.Tests/Services/VendorProductDataScopeTests.cs
git commit -m "Add VendorProductDataScope for the Vendor host (ARCH-001 Phase 1)"
```

**Note:** `Grand.Web.Vendor/Extensions/HasAccess.cs:19` (`HasAccessToProduct`) stays as-is — it's still used by other Vendor controllers (Order, Shipment, etc. also call sibling `HasAccessTo*` methods from the same file) that are out of scope for this plan. Do not delete or modify that file in this plan.

---

## Task 5: DI registration in all three hosts

**Files:**
- Modify: `src/Web/Grand.Web.Admin/Startup/*.cs` (find via Step 1 below)
- Modify: `src/Web/Grand.Web.Store/Startup/*.cs`
- Modify: `src/Web/Grand.Web.Vendor/Startup/*.cs`

**Interfaces:**
- Consumes: `GlobalAdminDataScope<TEntity>`, `StoreAdminDataScope<TEntity>`, `VendorProductDataScope` (Tasks 2-4).
- Produces: `IAdminDataScope<Product>` resolvable via DI in each host — consumed by `BaseProductController` (Task 7) and the shared `ProductViewModelService` (Task 9).

- [ ] **Step 1: Find where each host registers its own services today**

Run: `grep -rln "AddScoped<IProductViewModelService" src/Web/Grand.Web.Admin src/Web/Grand.Web.Store src/Web/Grand.Web.Vendor`

This locates the `IStartupApplication` (or equivalent DI extension) file per host where the Product-related registration already lives — register the new scope next to it.

- [ ] **Step 2: Register in Admin**

In the file found for `Grand.Web.Admin`, add:
```csharp
services.AddScoped<IAdminDataScope<Product>, GlobalAdminDataScope<Product>>();
```
(Add `using Grand.Web.AdminShared.Interfaces;`, `using Grand.Web.AdminShared.Services;`, `using Grand.Domain.Catalog;` if not already present.)

- [ ] **Step 3: Register in Store**

In the file found for `Grand.Web.Store`, add:
```csharp
services.AddScoped<IAdminDataScope<Product>, StoreAdminDataScope<Product>>();
```

- [ ] **Step 4: Register in Vendor**

In the file found for `Grand.Web.Vendor`, add:
```csharp
services.AddScoped<IAdminDataScope<Product>, VendorProductDataScope>();
```

- [ ] **Step 5: Build all three hosts**

Run:
```
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj
dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```
Expected: all succeed (registration has no consumers yet, so this only proves the DI call compiles).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Register IAdminDataScope<Product> in Admin, Store, Vendor hosts (ARCH-001 Phase 1)"
```

---

## Task 6: Resource-key-prefix audit for the Product vertical

**Files:**
- No source changes — this task produces a decision table consumed by Tasks 7-10.

**Interfaces:** none.

**Why:** confirmed by direct comparison of `PrepareProductListModel` in AdminShared vs Vendor — Vendor's copy uses `"Vendor.Common.All"` / `"Vendor.Catalog.Products.List.SearchPublished.*"` where AdminShared's uses `"Admin.*"`. `ResourceKeyPrefix` (Task 1) exists to make this swap mechanical, but **do not assume every `Admin.X` key has a matching `Vendor.X` key** — some resources may only exist under one prefix, in which case the key must stay a literal, not be templated.

- [ ] **Step 1: Extract every resource key literal referenced by the three controllers and both services**

Run (from repo root):
```
grep -ohE 'GetResource\("[A-Za-z]+\.[^"]+"\)' \
  src/Web/Grand.Web.Admin/Controllers/ProductController.cs \
  src/Web/Grand.Web.Store/Controllers/ProductController.cs \
  src/Web/Grand.Web.Vendor/Controllers/ProductController.cs \
  src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs \
  src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs \
  | sed -E 's/GetResource\("([A-Za-z]+)\.([^"]+)"\)/\1|\2/' \
  | sort -u > /tmp/product-resource-keys.txt
```

- [ ] **Step 2: Group by suffix (the part after the first `.`) and diff prefixes**

For each unique suffix, note which prefixes (`Admin`, `Store`, `Vendor`) appear. Three outcomes:
1. Only `Admin` appears (e.g. today's Store host, which has no separate resource set) → keep the literal `"Admin.<suffix>"` in the shared code; do not template it.
2. Both `Admin` and `Vendor` appear for the same suffix (confirmed case: `Common.All`, `Catalog.Products.List.SearchPublished.*`, `Catalog.Products.Added`, `Catalog.Products.Updated`, `Catalog.Products.Deleted`, `Catalog.Products.Fields.ChangedWarning`, `Catalog.Products.Permissions`) → template as `$"{scope.ResourceKeyPrefix}.<suffix>"`.
3. A prefix+suffix combination appears in only one host with no equivalent elsewhere and looks like a real per-host resource (not just a stray) → keep it as a literal guarded by `scope.ResourceKeyPrefix == "Vendor"` (or an `if`/virtual override) rather than templating — templating here would silently look up a resource key that doesn't exist for other hosts, which renders as the raw key text.

- [ ] **Step 3: Save the table as a code comment**

Add the resulting suffix → outcome table as a comment block at the top of `BaseProductController` (created in Task 7) so it stays next to the code it governs, e.g.:
```csharp
// Resource-key-prefix audit (2026-08-16, ARCH-001 Phase 1 Task 6):
// Templated via {scope.ResourceKeyPrefix}: Common.All, Catalog.Products.List.SearchPublished.*,
//   Catalog.Products.{Added,Updated,Deleted}, Catalog.Products.Fields.ChangedWarning,
//   Catalog.Products.Permissions.
// Admin-only literal (Store has no separate resource set): <fill in from Step 2>.
// Host-specific, not templated: <fill in from Step 2>.
```

No commit for this task alone — its output lands inside Task 7's commit.

---

## Task 7: `BaseProductController` skeleton + worked region ("Product list / create / edit / delete")

This is the template every remaining region in Task 8 follows. Do this one region fully and correctly before touching any other region.

**Files:**
- Create: `src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs`
- Test: `src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs` (new file — see Task 13 for what happens to the three existing per-host `ProductControllerTests.cs` files)

**Interfaces:**
- Consumes: `IAdminDataScope<Product>` (Task 1, registered per-host in Task 5), existing `IProductViewModelService`, `IProductService`, and the other 8 services already injected by all three current controllers (see the three constructors — they are identical in shape).
- Produces: `BaseProductController` — subclassed by all three hosts in Task 11.

- [ ] **Step 1: Reconcile the three current `List`/`Create`/`Edit`/`Delete`/`CopyProduct` bodies**

Read (already done for this plan — reproduced here for reference, do not re-read unless verifying):
- `src/Web/Grand.Web.Admin/Controllers/ProductController.cs:78-291`
- `src/Web/Grand.Web.Store/Controllers/ProductController.cs:94-...` (equivalent region, includes the `CanAccessProduct` helper at line 88)
- `src/Web/Grand.Web.Vendor/Controllers/ProductController.cs:91-307` (includes the `CheckAccessToProduct` helper at line 81)

Differences found, and how each is resolved:
| Difference | Admin | Store | Vendor | Resolution |
|---|---|---|---|---|
| Access check on `Edit`/`Delete`/`CopyProduct` | none | `CanAccessProduct` (`AccessToEntityByStore`) | `CheckAccessToProduct` (`HasAccessToProduct`) | `await scope.HasAccess(product)` |
| `GoToSku` access check | none | checks `CanAccessProduct` (note: current Store code has a **pre-existing bug** — it redirects to `Edit` regardless of whether `CanAccessProduct` returns true or false; preserve as a separate follow-up, do not silently fix inside this refactor — see Step 1a) | none | `if (product != null) { if (!await scope.HasAccess(product)) { /* preserve existing per-host behavior, see 1a */ } return RedirectToAction("Edit", new { id = product.Id }); }` |
| `List()` storeId arg | `PrepareProductListModel()` | `PrepareProductListModel(StaffStoreId)` | `PrepareProductListModel()` | `PrepareProductListModel(scope.DefaultStoreId ?? "")` once Task 9 lands; until then keep the current 4 arities separate per interface signature |
| `Create()` GET default `model.StoreId` | not set | `= StaffStoreId` | not set | `model.StoreId = scope.DefaultStoreId;` (no-op when null) |
| `Create()`/`Edit()` POST `model.Stores`/`model.StoreId` stamping | not set | `[StaffStoreId]` / `StaffStoreId` | not set | `if (scope.DefaultStoreId is not null) { model.Stores = [scope.DefaultStoreId]; model.StoreId = scope.DefaultStoreId; }` |
| `Edit()` GET extra "still has other stores" warning branch (Store only, lines 184-194) | absent | present | absent | Keep as a `protected virtual` no-op hook `EditWarningCheck(Product product)` overridden only in the Store subclass (Task 11) — this is host UI copy behavior, not scope logic, so it does not belong in `IAdminDataScope`. |
| `PrepareProductModel(model, product, bool, bool)` arity | 4-arg | 4-arg | **3-arg** (no `excludeProperties`) | Resolved by Task 9 (interface unification) — until Task 9 lands, `BaseProductController` calls the 4-arg AdminShared signature with `excludeProperties: false` as Vendor's implicit default; verify against Vendor's actual usage (`false` in `Create()`, `true` in the redisplay-on-invalid branches) before assuming — re-check `src/Web/Grand.Web.Vendor/Controllers/ProductController.cs:139,157,174,222` line by line. |
| Resource key prefix | `"Admin.*"` | `"Admin.*"` | `"Vendor.*"` | `$"{scope.ResourceKeyPrefix}.Catalog.Products.Added"` etc., per Task 6's table |
| `DeleteSelected` | present, no controller-level filter | absent | present, no controller-level filter (Vendor's *service* — `Grand.Web.Vendor/Services/ProductViewModelService.cs:687` — filters per-id by `HasAccessToProduct`, but the controller doesn't) | **Not harmless** — MVC routes actions regardless of whether a host's views link to them, so shipping this unfiltered into the shared base hands Store a brand-new unscoped bulk-delete endpoint once Task 11 subclasses it, and leaves Vendor's only protection sitting in one host's service rather than the controller like every other guarded action here. Filter ids through `scope.HasAccess` in the base controller before delegating — see the code below. |

- [ ] **Step 1a: File a note, don't fix, the Store `GoToSku` bug found above**

Add a `// TODO(ARCH-001-followup):` comment at the merged `GoToSku` call site describing the found inconsistency (Store's current code redirects to Edit on both branches of the access check) and leave the **new, merged** behavior doing the historically-safer thing: redirect to `List` (not `Edit`) when `HasAccess` is false, matching Vendor's stricter pattern, since silently landing on the edit screen of a product you don't have access to is the more dangerous default. Call this out explicitly in the PR description this task's commit goes into — this is a deliberate behavior tightening, not an accidental one.

- [ ] **Step 2: Write `BaseProductController` with this region only**

```csharp
using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;

namespace Grand.Web.AdminShared.Controllers;

// Resource-key-prefix audit (2026-08-16, ARCH-001 Phase 1 Task 6, corrected after review — see Task 6's
// ledger entry). Inlined in full here (not just referenced) since planning artifacts under .superpowers/
// are untracked and do not survive in the repo once this branch merges.
//
// Templated via {scope.ResourceKeyPrefix} (Admin.<suffix> and Vendor.<suffix> both exist) — 22:
//   Common.All, Customers.Guest, Configuration.Tax.Settings.TaxCategories.None,
//   Catalog.Products.Added, Catalog.Products.Updated, Catalog.Products.Deleted,
//   Catalog.Products.Fields.ChangedWarning, Catalog.Products.Fields.DeliveryDate.None,
//   Catalog.Products.Fields.Warehouse.None, Catalog.Products.Bids.CantDeleteWithOrder,
//   Catalog.Products.List.SkuNotFound, Catalog.Products.List.SearchPublished.All,
//   Catalog.Products.List.SearchPublished.PublishedOnly, Catalog.Products.List.SearchPublished.UnpublishedOnly,
//   Catalog.Products.List.SearchPublished.MarkAsNew, Catalog.ProductReservations.CantDeleteWithOrder,
//   Catalog.Products.Calendar.CannotChangeInterval,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.MinLength,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.MaxLength,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileAllowedExtensions,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.FileMaximumSize,
//   Catalog.Products.ProductAttributes.Attributes.ValidationRules.DefaultValue.
//
// Admin-only literal (no Vendor equivalent call site; keep as literal "Admin.<suffix>") — 6:
//   Catalog.Products.Permissions (Vendor has no Permissions-suffixed resource lookup anywhere - its
//     permission-denied paths don't emit this message), Catalog.Products.List.SearchPublished.ShowOnHomePage,
//   Catalog.Products.Imported, Catalog.Products.TierPrices.Fields.CustomerGroup.All,
//   Catalog.Products.TierPrices.Fields.Store.All, Common.UploadFile.
//
// Host-specific, not templated — 0: none found; every "Vendor.<suffix>" call site has a matching
//   "Admin.<suffix>" one, so nothing needs a scope.ResourceKeyPrefix == "Vendor" guard instead of templating.
//
// Store makes no separate resource lookups at all - every Store call site uses the literal "Admin.*" key
// directly (Store has no distinct resource set), consistent with StoreAdminDataScope.ResourceKeyPrefix
// returning "Admin".

[PermissionAuthorize(PermissionSystemName.Products)]
public abstract class BaseProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseController
{
    /// <summary>Hook for host-specific UI-copy warnings that aren't access-scope decisions.
    /// Overridden by the Store subclass; no-op everywhere else.</summary>
    protected virtual void EditWarningCheck(Product product) { }

    #region Product list / create / edit / delete

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        var model = await productViewModelService.PrepareProductListModel(scope.DefaultStoreId ?? "");
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ProductList(DataSourceRequest command, ProductListModel model)
    {
        if (scope.DefaultStoreId is not null) model.SearchStoreId = scope.DefaultStoreId;

        var (productModels, totalCount) =
            await productViewModelService.PrepareProductsModel(model, command.Page, command.PageSize);
        return Json(new DataSourceResult { Data = productModels.ToList(), Total = totalCount });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToSku(ProductListModel model)
    {
        var product = await productService.GetProductBySku(model.GoDirectlyToSku);
        if (product != null)
        {
            if (!await scope.HasAccess(product))
                return RedirectToAction("List", "Product"); // TODO(ARCH-001-followup): see Task 7 Step 1a
            return RedirectToAction("Edit", "Product", new { id = product.Id });
        }

        Warning(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SkuNotFound"));
        return RedirectToAction("List", "Product");
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ProductModel { StoreId = scope.DefaultStoreId };
        await productViewModelService.PrepareProductModel(model, null, true, true);
        await AddLocales(languageService, model.Locales);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(ProductModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.StoreId = scope.DefaultStoreId;
            }

            var product = await productViewModelService.InsertProductModel(model);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
        }

        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, null, false, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var product = await productService.GetProductById(id, true);
        if (product == null) return RedirectToAction("List");

        EditWarningCheck(product);
        // CanView, not HasAccess: viewing a shared/global product is allowed on Store (with a warning
        // from EditWarningCheck above); only mutating one is restricted to the exclusive single-store
        // owner. See IAdminDataScope<TEntity>.CanView's doc comment and Task 3's addendum.
        if (!await scope.CanView(product)) return RedirectToAction("List");

        var model = product.ToModel(dateTimeService);
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, product, false, false);
        await AddLocales(languageService, model.Locales, (locale, languageId) =>
        {
            locale.Name = product.GetTranslation(x => x.Name, languageId, false);
            locale.ShortDescription = product.GetTranslation(x => x.ShortDescription, languageId, false);
            locale.FullDescription = product.GetTranslation(x => x.FullDescription, languageId, false);
            locale.MetaKeywords = product.GetTranslation(x => x.MetaKeywords, languageId, false);
            locale.MetaDescription = product.GetTranslation(x => x.MetaDescription, languageId, false);
            locale.MetaTitle = product.GetTranslation(x => x.MetaTitle, languageId, false);
            locale.SeName = product.GetSeName(languageId, false);
        });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(ProductModel model, bool continueEditing)
    {
        var product = await productService.GetProductById(model.Id, true);
        if (product == null) return RedirectToAction("List");
        if (!await scope.HasAccess(product)) return RedirectToAction("Edit", new { id = product.Id });

        if (model.Ticks != product.Ticks)
        {
            Error(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Fields.ChangedWarning"));
            return RedirectToAction("Edit", new { id = product.Id });
        }

        if (ModelState.IsValid)
        {
            if (scope.DefaultStoreId is not null)
            {
                model.Stores = [scope.DefaultStoreId];
                model.StoreId = scope.DefaultStoreId;
            }

            product = await productViewModelService.UpdateProductModel(product, model);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Updated"));
            if (continueEditing)
            {
                await SaveSelectedTabIndex();
                return RedirectToAction("Edit", new { id = product.Id });
            }

            return RedirectToAction("List");
        }

        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        await productViewModelService.PrepareProductModel(model, product, false, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var product = await productService.GetProductById(id, true);
        if (product == null) return RedirectToAction("List");
        if (!await scope.HasAccess(product)) return RedirectToAction("Edit", new { id });

        if (ModelState.IsValid)
        {
            await productViewModelService.DeleteProduct(product);
            Success(translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
    {
        if (selectedIds == null || selectedIds.Count == 0) return Json(new { Result = true });

        // This is a mutation (bulk delete), so it uses the strict HasAccess, matching Edit(POST)/Delete
        // above — not a no-op pass-through to the service. Without this filter, Store gains an
        // unscoped bulk-delete endpoint (any store staff could delete any product id in the system,
        // bypassing AccessToEntityByStore entirely) purely because MVC routes actions regardless of
        // whether a host's views ever link to them. See Task 7's review for the full analysis.
        var products = await productService.GetProductsByIds(selectedIds.ToArray(), true);
        var allowedIds = new List<string>();
        foreach (var product in products)
            if (await scope.HasAccess(product))
                allowedIds.Add(product.Id);

        if (allowedIds.Count > 0) await productViewModelService.DeleteSelected(allowedIds);
        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    public async Task<IActionResult> CopyProduct(ProductModel model,
        [FromServices] ICopyProductService copyProductService, [FromServices] IPictureService pictureService)
    {
        var copyModel = model.CopyProductModel;
        try
        {
            var originalProduct = await productService.GetProductById(copyModel.Id, true);
            // CanView, not HasAccess: Store's original CopyProduct denies only when LimitedToStores is
            // true AND the staff member's store isn't among them — the same looser rule as Edit(GET)
            // above, not the strict mutation rule. See IAdminDataScope<TEntity>.CanView.
            if (!await scope.CanView(originalProduct)) return RedirectToAction("List");

            if (scope.DefaultStoreId is not null)
            {
                originalProduct.LimitedToStores = true;
                originalProduct.Stores.Clear();
                originalProduct.Stores.Add(scope.DefaultStoreId);
            }

            var newProduct = await copyProductService.CopyProduct(originalProduct, copyModel.Name, copyModel.Published);
            if (copyModel.CopyImages) await CopyImages(originalProduct, newProduct, pictureService);

            Success("The product has been copied successfully");
            return RedirectToAction("Edit", new { id = newProduct.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return RedirectToAction("Edit", new { id = copyModel.Id });
        }
    }

    private async Task CopyImages(Product originalProduct, Product newProduct, IPictureService pictureService)
    {
        foreach (var productPicture in originalProduct.ProductPictures)
        {
            var picture = await pictureService.GetPictureById(productPicture.PictureId);
            var pictureCopy = await pictureService.InsertPicture(
                await pictureService.LoadPictureBinary(picture),
                picture.MimeType,
                pictureService.GetPictureSeName(newProduct.Name),
                picture.AltAttribute,
                picture.TitleAttribute,
                false,
                Reference.Product,
                newProduct.Id);

            await productService.InsertProductPicture(new ProductPicture {
                PictureId = pictureCopy.Id,
                DisplayOrder = productPicture.DisplayOrder,
                IsDefault = productPicture.IsDefault
            }, newProduct.Id);
        }
    }

    #endregion
}
```

Note: `originalProduct.VendorId` is untouched by `CopyProduct` above — verify against Vendor's actual current behavior (does a vendor-copied product get `VendorId` stamped anywhere, e.g. inside `copyProductService.CopyProduct`?) during Step 1; if Vendor's controller relies on `CopyProduct` internally reading `IWorkContext.CurrentVendor`, no controller-level change is needed and this note can be deleted.

- [ ] **Step 3: Write `BaseProductControllerTests` covering the merged access-check behavior**

Port the `Edit`/`Delete`/`CopyProduct`/`GoToSku` access-denied and access-granted test cases already present across the three existing `ProductControllerTests.cs` files (Task 0) into this one file, parameterized over a mocked `IAdminDataScope<Product>` instead of three different concrete access mechanisms. Use `Moq` to set up `scope.HasAccess(...)` returning `true`/`false` per case — this replaces, rather than duplicates, the equivalent cases in the per-host files (removed in Task 13).

- [ ] **Step 4: Run the new tests**

Run: `dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~BaseProductControllerTests"`
Expected: PASS. `BaseProductController` is abstract and has no host yet, so these tests instantiate it via a minimal test-only subclass (e.g. `private class TestProductController(...) : BaseProductController(...)`) if MSTest/Moq cannot mock an abstract class directly for the actions under test.

- [ ] **Step 5: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs
git commit -m "Add BaseProductController with the list/create/edit/delete region (ARCH-001 Phase 1)"
```

Do **not** proceed to Task 11 (host subclasses) yet — `BaseProductController` is incomplete until Task 8 migrates the remaining 23 regions.

---

## Task 8: Migrate the remaining 23 `#region`s into `BaseProductController`

One region per checklist row, each following Task 7's template exactly: read the region in all three current controllers, build the difference table, resolve every difference through `scope`/`ResourceKeyPrefix`/a `protected virtual` hook, append the merged region to `BaseProductController`, port the corresponding test cases, run tests, commit. Each row is independently testable and commit-able — do not batch multiple rows into one commit.

**Files (per row):**
- Modify: `src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs` (append the region)
- Modify: `src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs` (append test cases for the region)
- Read: the matching region in all three of `src/Web/Grand.Web.{Admin,Store,Vendor}/Controllers/ProductController.cs`, located by the line numbers below (current as of this plan's authoring — re-locate by region name if line numbers have drifted).

| # | Region | Admin start line | Store start line* | Vendor start line* |
|---|---|---|---|---|
| 1 | Required products | 293 | (grep `#region Required products`) | (grep) |
| 2 | Product categories | 347 | " | " |
| 3 | Product collections | 415 | " | " |
| 4 | Related products | 483 | " | " |
| 5 | Similar products | 576 | " | " |
| 6 | Bundle products | 669 | " | " |
| 7 | Cross-sell products | 762 | " | " |
| 8 | Recommended products | 845 | " | " |
| 9 | Associated products | 927 | " | " |
| 10 | Product pictures | 1029 | " | " |
| 11 | Product specification attributes | 1161 | " | " |
| 12 | Purchased with order | 1278 | " | " |
| 13 | Reviews | 1308 | " | " |
| 14 | Export / Import | 1338 | " | " |
| 15 | Bulk editing | 1406 | " | " |
| 16 | Product currency price | 1446 | " | " |
| 17 | Tier prices | 1559 | " | " |
| 18 | Product attributes | 1673 | " | " |
| 19 | Product attributes. Condition | 1791 | " | " |
| 20 | Product attribute values | 1828 | " | " |
| 21 | Product attribute combinations | 2031 | " | " |
| 22 | Product Attribute combination - tier prices | 2139 | " | " |
| 23 | Reservation | 2219 | " | " |
| 24 | Bids | 2433 | " | " |

*Store and Vendor line numbers were not pre-extracted for every region (only the shared region list, confirmed identical region names/order across all three files via `grep -n "#region" <file>` on 2026-08-16). Locate each region in Store/Vendor with `grep -n "#region <name>" src/Web/Grand.Web.{Store,Vendor}/Controllers/ProductController.cs` at the start of that row's work — do not assume the same line number as Admin.

- [ ] **Step 1 (repeat per row): Read the region in all three controllers and build the difference table**

Same procedure as Task 7 Step 1. Pay special attention to:
- Any `_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId` or `.CurrentVendor` reference → route through `scope`.
- Any `HasAccessToProduct`/`CanAccessProduct`/`CheckAccessToProduct` call → `await scope.HasAccess(...)`.
- Any `"Admin.*"` / `"Vendor.*"` resource key → check against Task 6's table before templating.
- Any region present in only one or two of the three controllers (not all 24 regions are guaranteed to exist verbatim in all three — confirm with `grep -c "#region" <file>` per host before assuming symmetry; a region missing from one host means that host's subclass simply never routes to those actions, which is fine — the shared method still exists, just unused by that host's views).

- [ ] **Step 2 (repeat per row): Append the merged region and its tests, run tests, commit**

```bash
dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~BaseProductControllerTests"
git add src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs
git commit -m "Migrate '<region name>' region into BaseProductController (ARCH-001 Phase 1)"
```

- [ ] **Step 3: After all 24 rows are committed, confirm `BaseProductController`'s member list is a superset of all three original controllers' public actions**

Run:
```
grep -oE "public (async )?(Task|IActionResult|JsonResult)[^(]*\(" src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs | sort -u > /tmp/base.txt
grep -oE "public (async )?(Task|IActionResult|JsonResult)[^(]*\(" src/Web/Grand.Web.Admin/Controllers/ProductController.cs | sort -u > /tmp/admin.txt
grep -oE "public (async )?(Task|IActionResult|JsonResult)[^(]*\(" src/Web/Grand.Web.Store/Controllers/ProductController.cs | sort -u > /tmp/store.txt
grep -oE "public (async )?(Task|IActionResult|JsonResult)[^(]*\(" src/Web/Grand.Web.Vendor/Controllers/ProductController.cs | sort -u > /tmp/vendor.txt
diff /tmp/base.txt /tmp/admin.txt
diff /tmp/base.txt /tmp/store.txt
diff /tmp/base.txt /tmp/vendor.txt
```
Expected: no unexplained diffs (action name + parameter type list should match; return-type wrapper differences like `Task<IActionResult>` vs `IActionResult` are fine to differ if intentional). Any method present in an old controller but missing from `BaseProductController` is a gap — go back and migrate it before moving to Task 9.

---

## Task 9: Unify `IProductViewModelService` — drop `storeId` params, inject `IAdminDataScope<Product>`, worked method (`PrepareProductListModel`)

**Files:**
- Modify: `src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs`
- Modify: `src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs`
- Modify: `src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs` — every call site of the 13 methods listed in Step 1 that Task 8 already migrated into a region (Required products, Related/Similar/Bundle/Cross-sell/Recommended/Associated products, Bulk editing, Tier prices, the attribute-value-association popup) loses its `storeId`/`scope.DefaultStoreId` argument in this same task. Find them with `grep -n "scope.DefaultStoreId" src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs` before starting Step 2 below, and fix every hit that calls one of the 13 methods.
- Test: `src/Tests/Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs` (existing file, extend)
- Test: `src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs` — update any test asserting the old arity for the 13 methods

**Interfaces:**
- Consumes: `IAdminDataScope<Product>` (Task 1).
- Produces: `IProductViewModelService.PrepareProductListModel()` (no `storeId` parameter — scope comes from the injected `IAdminDataScope<Product>` instead) — consumers updated in Task 8's regions and `BaseProductController.List()` (Task 7, revisit).

**Design decision (confirmed by comparing the two current interfaces at `src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs` vs `src/Web/Grand.Web.Vendor/Interfaces/IProductViewModelService.cs`):** AdminShared's interface threads scope through explicit `storeId` parameters on ~10 methods; Vendor's interface has no such parameters and instead reads `IWorkContext.CurrentVendor` internally. Converging on **caller-supplied `storeId` parameters is wrong for Vendor** (vendor scope isn't a store id at all) — converge on **DI-injected `IAdminDataScope<Product>` inside the service**, matching what `BaseProductController` already does, and remove the `storeId` parameters entirely. This changes the public interface — every call site found in Task 8 must be updated in the same commit as the interface change to keep the build green (do this task before, or interleaved with, whichever Task 8 rows call an affected method — recommend doing Task 9 immediately after Task 8's `#region Product list / create / edit / delete`-adjacent rows are done, since `PrepareProductListModel`/`PrepareProductsModel` are used there).

- [ ] **Step 1: List every interface method with a `storeId` parameter**

From the interface diff (already gathered for this plan):
```
PrepareProductModel(ProductModel, Product, bool, bool)          // excludeProperties: keep 4-arg, see Task 7 Step 1
PrepareTierPriceModel(ProductModel.TierPriceModel, string storeId = "")
PrepareProductListModel(string storeId = "")
PrepareAddRequiredProductModel(string storeId = "")
PrepareRelatedProductModel(string storeId = "")
PrepareSimilarProductModel(string storeId = "")
PrepareBundleProductModel(string storeId = "")
PrepareCrossSellProductModel(string storeId = "")
PrepareRecommendedProductModel(string storeId = "")
PrepareAssociatedProductModel(string storeId = "")
PrepareBulkEditListModel(string storeId = "")
PrepareTierPriceModel(Product, string storeId = "")
PrepareAssociateProductToAttributeValueModel(string storeId = "")
```
Also reconcile the three non-storeId signature mismatches found in the diff:
```
OutOfStockNotifications(Product, ProductModel, int)   // AdminShared        vs   OutOfStockNotifications(Product, int)   // Vendor — extra ProductModel param, verify whether Vendor's body needs it or genuinely doesn't
UpdateProductSpecificationAttributeModel(Product, ProductSpecificationAttribute, ...)  // AdminShared  vs  (ProductSpecificationAttribute, ...)  // Vendor — extra Product param
```
These two need their bodies read (not just signatures) during this task — read `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs` at the matching method names to see whether the omitted parameter is actually unused or silently sourced from elsewhere (e.g. re-fetched inside the method).

- [ ] **Step 2: Remove `storeId` from every method above in `IProductViewModelService.cs`**

Example (repeat pattern for all 13 methods):
```csharp
// before
Task<ProductListModel> PrepareProductListModel(string storeId = "");
// after
Task<ProductListModel> PrepareProductListModel();
```

- [ ] **Step 3: Add `IAdminDataScope<Product> scope` to the service's primary constructor and rewrite `PrepareProductListModel`**

```csharp
public virtual async Task<ProductListModel> PrepareProductListModel()
{
    var model = new ProductListModel();
    var storeId = scope.DefaultStoreId ?? "";

    model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = " " });
    foreach (var s in (await storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
        model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

    model.AvailableWarehouses.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = " " });
    foreach (var wh in await warehouseService.GetAllWarehouses(storeId))
        model.AvailableWarehouses.Add(new SelectListItem { Text = wh.Name, Value = wh.Id });

    model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
    model.AvailableProductTypes.Insert(0, new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "0" });

    model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.All"), Value = " " });
    model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.PublishedOnly"), Value = "1" });
    model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.UnpublishedOnly"), Value = "2" });

    // Admin/Store show "Show on homepage" (value 3); Vendor's current copy omits it entirely (vendors can't
    // feature products on the homepage — a real capability difference, not a naming difference). Gate it:
    if (scope.ResourceKeyPrefix != "Vendor")
        model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.ShowOnHomePage"), Value = "3" });

    model.AvailablePublishedOptions.Add(new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Catalog.Products.List.SearchPublished.MarkAsNew"), Value = "4" });

    return model;
}
```

Note the `if (scope.ResourceKeyPrefix != "Vendor")` gate: this is a real capability difference (found by reading both bodies for this plan — Vendor's method has no "Show on homepage" option at all, Admin/Store do), not just a resource-prefix difference. Using `ResourceKeyPrefix` as the gate condition here is a shortcut that works today (only Vendor differs) but is semantically about capability, not localization — if a fourth host is ever added, replace this with a proper `bool CanFeatureOnHomepage` on `IAdminDataScope<Product>` rather than continuing to overload `ResourceKeyPrefix` for behavior gating. Leave a comment saying so.

Also drop the `model.AvailableStores` population entirely when `scope is VendorProductDataScope`-equivalent (Vendor's original method has no store dropdown at all — vendors don't pick stores). Since `DefaultStoreId` is `null` for Vendor already, `storeId` will be `""` for Vendor same as Admin — that would incorrectly populate the stores dropdown for Vendor. Add an explicit capability flag instead of overloading `DefaultStoreId is null` (which is also true for Admin, where the dropdown *should* show): introduce a fourth member on `IAdminDataScope<TEntity>` — `bool ShowStoreSelector { get; }` (`true` for Admin, `true` for Store, `false` for Vendor) — go back to Task 1-4 and add it now:
```csharp
// IAdminDataScope<TEntity> addition:
bool ShowStoreSelector { get; }
// GlobalAdminDataScope: => true;  StoreAdminDataScope: => true;  VendorProductDataScope: => false;
```
Update Task 1-4's tests to cover this new member (one assertion each) before continuing.

- [ ] **Step 4: Update the two call sites already migrated in Task 7 (`BaseProductController.List()`)**

```csharp
// before (Task 7)
var model = await productViewModelService.PrepareProductListModel(scope.DefaultStoreId ?? "");
// after
var model = await productViewModelService.PrepareProductListModel();
```

- [ ] **Step 5: Run the full Admin service test suite**

Run: `dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~ProductViewModelServiceTests"`
Expected: existing `PrepareProductListModel` tests need updating for the new no-arg signature — update them to construct the service with a mocked `IAdminDataScope<Product>` (three variants: default/global, store-scoped, vendor-scoped) instead of passing a `storeId` string, and add a case asserting the homepage option and store dropdown are absent when the mock reports `ResourceKeyPrefix == "Vendor"` / `ShowStoreSelector == false`.

- [ ] **Step 6: Commit**

```bash
git add src/Web/Grand.Web.AdminShared/Interfaces/IAdminDataScope.cs src/Web/Grand.Web.AdminShared/Services/GlobalAdminDataScope.cs src/Web/Grand.Web.AdminShared/Services/StoreAdminDataScope.cs src/Web/Grand.Web.AdminShared/Services/VendorProductDataScope.cs src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs src/Tests/Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs src/Tests/*/Services/*DataScopeTests.cs
git commit -m "Unify IProductViewModelService onto IAdminDataScope<Product>, drop storeId params; add ShowStoreSelector (ARCH-001 Phase 1)"
```

This will not build yet — `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs` still implements the *old* Vendor-only `IProductViewModelService` (different interface, different namespace) and hasn't been touched. It stays broken/unreferenced until Task 12 deletes it. Confirm the build error is scoped to that one file (`dotnet build Grand.Web.AdminShared.csproj` should succeed on its own) before committing.

---

## Task 10: Reconcile the remaining ~29 `ProductViewModelService` methods

Same per-row discipline as Task 8: one method (or tightly-coupled small group, e.g. the three attribute-value overloads) per row, each read in both AdminShared and Vendor, differences resolved via `scope`, tested, committed independently.

**Files (per row):**
- Modify: `src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs`
- Modify: `src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs` (drop any remaining `storeId` param for that method)
- Modify (when the row drops a `storeId` param): `src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs` — grep for the method name and update every call site the same way Task 9 did for `PrepareProductListModel`.
- Test: `src/Tests/Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs`
- Test (when a call site changed): `src/Tests/Grand.Web.Admin.Tests/Controllers/BaseProductControllerTests.cs`
- Read: `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs` at the matching method (method names are shared — same name, different arity/body — locate with `grep -n "MethodName" src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs`).

**Checklist (from the AdminShared method list; each unchecked row still has a `storeId` param or an unverified arity mismatch with Vendor per Task 9 Step 1):**
- [ ] `PrepareAddProductAttributeCombinationModel`
- [ ] `PrepareTierPriceModel(ProductModel.TierPriceModel, storeId)` — drop `storeId`
- [ ] `PrepareProductAttributeValueModel(Product, ...)`
- [ ] `PrepareProductModel(ProductModel, Product, bool, bool)` — resolve the 4-arg/3-arg `excludeProperties` mismatch flagged in Task 7 Step 1
- [ ] `PrepareProductReviewModel`
- [ ] `PrepareProductsModel`
- [ ] `PrepareProducts(ProductListModel)`
- [ ] `PrepareAddRequiredProductModel(storeId)` — drop `storeId`
- [ ] `PrepareProductModel(DataSourceRequest-ish overload at line 816)`
- [ ] `PrepareProductCategoryModel`
- [ ] `PrepareProductCollectionModel`
- [ ] `PrepareRelatedProductModel(storeId)` — drop `storeId`
- [ ] `PrepareSimilarProductModel(storeId)` — drop `storeId`
- [ ] `PrepareBundleProductModel(storeId)` — drop `storeId`
- [ ] `PrepareCrossSellProductModel(storeId)` — drop `storeId`
- [ ] `PrepareRecommendedProductModel(storeId)` — drop `storeId`
- [ ] `PrepareAssociatedProductModel(storeId)` — drop `storeId`
- [ ] `PrepareBulkEditListModel(storeId)` — drop `storeId`
- [ ] `PrepareBulkEditProductModel` — **hard prerequisite, not optional:** Vendor's original implementation (`src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs`) passes `vendorId: contextAccessor.WorkContext.CurrentVendor.Id` into the underlying `SearchProducts` call, vendor-scoping the bulk-edit grid; AdminShared's version and `BulkEditListModel` have no vendor-id field/parameter at all. `BaseProductController.BulkEditSelect` (Task 8 row 15, already migrated) routes to AdminShared's unfiltered version — this is fine today only because Vendor is not yet subclassed onto `BaseProductController`. **Task 11 must not wire Vendor's `ProductController` onto `BaseProductController` until this method gains vendor-scoped filtering** (e.g. via `scope.ApplyScope` on the underlying query, or an injected filter callback) — doing so first would silently let any vendor see every vendor's products in the bulk-edit grid. Flag this row's completion as blocking Task 11, not merely a nice-to-have cleanup.
- [ ] `PrepareTierPriceModel(Product, storeId)` — drop `storeId`
- [ ] `PrepareBidMode`
- [ ] `PrepareProductAttributeMappingModel` (4 overloads at lines 1365/1379/1392/1526 — AdminShared has one more overload than Vendor per the interface diff; confirm which one and whether Vendor needs it)
- [ ] `PrepareProductAttributeMappingModels`
- [ ] `PrepareProductAttributeConditionModel`
- [ ] `PrepareProductAttributeValueModel` (2 more overloads at 1715/1798)
- [ ] `PrepareProductAttributeValueModels`
- [ ] `PrepareAssociateProductToAttributeValueModel(storeId)` — drop `storeId`
- [ ] `PrepareProductAttributeCombinationModel`
- [ ] `PrepareProductPicturesModel`
- [ ] `PrepareProductPictureModel`
- [ ] `PrepareProductSpecificationAttributeModel`
- [ ] `OutOfStockNotifications` — resolve the `ProductModel` param mismatch flagged in Task 9 Step 1
- [ ] `UpdateProductSpecificationAttributeModel` — resolve the `Product` param mismatch flagged in Task 9 Step 1
- [ ] `InsertProductModel`, `UpdateProductModel`, `DeleteProduct`, `DeleteSelected` — confirm identical between AdminShared/Vendor already (likely candidates for "no change needed", but verify, don't assume)

- [ ] **Step 1 (repeat per row): read both bodies, resolve differences, update interface + implementation + tests, run tests, commit**

```bash
dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~ProductViewModelServiceTests"
git add src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs src/Tests/Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs
git commit -m "Reconcile <MethodName> in ProductViewModelService (ARCH-001 Phase 1)"
```

- [ ] **Step 2: After all rows are checked off, confirm no `storeId` parameters remain**

Run: `grep -n "storeId" src/Web/Grand.Web.AdminShared/Interfaces/IProductViewModelService.cs`
Expected: no matches (or only local variables inside method bodies that read `scope.DefaultStoreId`, not parameters).

---

## Task 11: Convert the three host `ProductController`s to thin subclasses

**Blocking prerequisite (added after Task 8 row 15's review):** do not subclass Vendor's `ProductController` onto `BaseProductController` until Task 10's `PrepareBulkEditProductModel` row is done and confirmed to preserve vendor-scoped filtering on the bulk-edit grid — see that row's note. Wiring Vendor on first would silently expose every vendor's products in `BulkEditSelect`'s grid to every other vendor. Admin and Store have no equivalent gap and can be subclassed independently of this prerequisite.

**Files:**
- Modify (rewrite, shrink): `src/Web/Grand.Web.Admin/Controllers/ProductController.cs`
- Modify (rewrite, shrink): `src/Web/Grand.Web.Store/Controllers/ProductController.cs`
- Modify (rewrite, shrink): `src/Web/Grand.Web.Vendor/Controllers/ProductController.cs`

**Interfaces:**
- Consumes: `BaseProductController` (Tasks 7-8, now complete with all 24 regions).

- [ ] **Step 1: Replace Admin's controller**

```csharp
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Localization;
using Grand.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[Area(Constants.AreaAdmin)]
public class ProductController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductReservationService productReservationService,
    IAuctionService auctionService,
    IDateTimeService dateTimeService,
    IPermissionService permissionService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseProductController(productViewModelService, productService, inventoryManageService, languageService,
        translationService, productReservationService, auctionService, dateTimeService, permissionService,
        enumTranslationService, scope);
```

(Fix the exact `using`s/namespaces for `IInventoryManageService`, `IProductReservationService`, `IAuctionService`, `IPermissionService`, `IEnumTranslationService` by copying them from the current file's `using` block before deleting it — don't re-guess namespaces not already confirmed in this plan.)

- [ ] **Step 2: Replace Store's controller the same way, plus the `EditWarningCheck` override**

```csharp
namespace Grand.Web.Store.Controllers;

[Area(Constants.AreaStore)]
public class ProductController(/* same params as Admin */) : BaseProductController(/* same args */)
{
    protected override void EditWarningCheck(Product product)
    {
        if (!product.LimitedToStores || (product.LimitedToStores && product.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Products.Permissions"));
    }
}
```
Re-derive the exact condition from the original Store code at `src/Web/Grand.Web.Store/Controllers/ProductController.cs:184-194` (reproduced in Task 7's Step 1 table) rather than retyping from memory — the original condition is unusual (it warns when NOT limited, or when limited AND accessible AND multi-store) and easy to get backwards. `TranslationService` needs to be exposed as a `protected` member on `BaseProductController` (it's currently a primary-constructor parameter, which C# does not expose to derived classes by name — add `protected ITranslationService TranslationService => translationService;` to `BaseProductController` in this step, or store a `protected readonly` field instead of a primary-constructor parameter for any member a subclass needs to reference).

- [ ] **Step 3: Replace Vendor's controller the same way**

No `EditWarningCheck` override needed (Vendor's original code has no equivalent branch).

- [ ] **Step 4: Build all three hosts**

Run:
```
dotnet build src/Web/Grand.Web.Admin/Grand.Web.Admin.csproj
dotnet build src/Web/Grand.Web.Store/Grand.Web.Store.csproj
dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj
```
Expected: Vendor still fails — it's still registering `Grand.Web.Vendor.Interfaces.IProductViewModelService` (old interface) rather than AdminShared's, and its own `ProductViewModelService` class still exists. That's resolved in Task 12; if Admin and Store also fail, stop and fix before proceeding (they should build clean at this point).

- [ ] **Step 5: Commit (Admin + Store only; hold Vendor's controller change until Task 12 makes it buildable)**

```bash
git add src/Web/Grand.Web.Admin/Controllers/ProductController.cs src/Web/Grand.Web.Store/Controllers/ProductController.cs src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs
git commit -m "Reduce Admin and Store ProductController to thin BaseProductController subclasses (ARCH-001 Phase 1)"
```

Vendor's rewritten controller from Step 3 stays as an uncommitted working-tree change until Task 12.

---

## Task 12: Delete Vendor's duplicate service and interface, finish Vendor's DI wiring

**Files:**
- Delete: `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs`
- Delete: `src/Web/Grand.Web.Vendor/Interfaces/IProductViewModelService.cs`
- Modify: Vendor's `Startup` DI registration file (found in Task 5 Step 1) — repoint `IProductViewModelService` registration to `Grand.Web.AdminShared.Services.ProductViewModelService` / `Grand.Web.AdminShared.Interfaces.IProductViewModelService`
- Modify: any other Vendor file referencing `Grand.Web.Vendor.Interfaces.IProductViewModelService` or `Grand.Web.Vendor.Models.Catalog.*` types that moved — find with Step 1

**Interfaces:**
- Consumes: `Grand.Web.AdminShared.Interfaces.IProductViewModelService` / `.Services.ProductViewModelService` (Tasks 9-10, now fully reconciled).

- [ ] **Step 1: Find every remaining reference to the old Vendor-local types**

Run:
```
grep -rln "Grand.Web.Vendor.Interfaces.IProductViewModelService\|Grand.Web.Vendor.Services.ProductViewModelService" src/Web/Grand.Web.Vendor --include=*.cs
grep -rln "using Grand.Web.Vendor.Interfaces;" src/Web/Grand.Web.Vendor/Controllers/ProductController.cs
```

- [ ] **Step 2: Delete the two files**

```bash
git rm src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs
git rm src/Web/Grand.Web.Vendor/Interfaces/IProductViewModelService.cs
```

- [ ] **Step 3: Update DI registration and every file found in Step 1 to reference AdminShared's interface/namespace instead**

Swap `using Grand.Web.Vendor.Interfaces;` → `using Grand.Web.AdminShared.Interfaces;` and `AddScoped<Grand.Web.Vendor.Interfaces.IProductViewModelService, ...>()` → `AddScoped<IProductViewModelService, ProductViewModelService>()` (AdminShared's), matching how Admin/Store already register it (check their `Startup` files for the exact existing line to mirror).

- [ ] **Step 4: Build Vendor**

Run: `dotnet build src/Web/Grand.Web.Vendor/Grand.Web.Vendor.csproj`
Expected: succeeds now.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Delete Vendor's duplicate ProductViewModelService/interface, use AdminShared's (ARCH-001 Phase 1)"
```

---

## Task 13: Consolidate characterization tests, delete superseded per-host duplicates

**Files:**
- Modify/trim: `src/Tests/Grand.Web.Admin.Tests/Controllers/ProductControllerTests.cs`
- Modify/trim: `src/Tests/Grand.Web.Store.Tests/Controllers/ProductControllerTests.cs`
- Modify/trim: `src/Tests/Grand.Web.Vendor.Tests/Controllers/ProductControllerTests.cs`
- Delete: `src/Tests/Grand.Web.Vendor.Tests/Services/ProductViewModelServiceTests.cs` (superseded by `Grand.Web.Admin.Tests/Services/ProductViewModelServiceTests.cs`, which now covers all scope variants per Tasks 9-10)

**Interfaces:** none new — this task only removes now-redundant test coverage and confirms the replacement (`BaseProductControllerTests`, extended `ProductViewModelServiceTests`) covers the same cases.

- [ ] **Step 1: For each of the three per-host `ProductControllerTests.cs`, identify which test cases exercised logic that now lives in `BaseProductController`**

Any test asserting scope/access-check behavior (e.g. "Edit returns RedirectToList when product belongs to another store") is now covered by `BaseProductControllerTests` (Task 7 Step 3, extended through Task 8). Any test asserting host-specific routing/area/authorization-attribute behavior only (e.g. "controller has `[Area(\"Admin\")]`") stays in the per-host file, since `BaseProductController` doesn't carry that attribute.

- [ ] **Step 2: Trim each per-host test file down to routing/attribute-only cases**

Remove the now-duplicated scope/business-logic cases; keep a small smoke test confirming the subclass resolves via DI and inherits the base actions, e.g.:
```csharp
[TestMethod]
public void ProductController_IsBaseProductController()
{
    Assert.IsInstanceOfType<BaseProductController>(CreateController());
}
```

- [ ] **Step 3: Delete the superseded Vendor service test file**

```bash
git rm src/Tests/Grand.Web.Vendor.Tests/Services/ProductViewModelServiceTests.cs
```

- [ ] **Step 4: Run every Product-related test across all three test projects**

Run:
```
dotnet test src/Tests/Grand.Web.Admin.Tests --filter "FullyQualifiedName~Product"
dotnet test src/Tests/Grand.Web.Store.Tests
dotnet test src/Tests/Grand.Web.Vendor.Tests --filter "FullyQualifiedName~Product"
```
Run `Grand.Web.Store.Tests` unfiltered — see the note in Task 0 about `AutoMapperConfig`'s static init living in `PaymentControllerTests`; a `~Product` filter on this project produces a false failure unrelated to this plan's changes.

Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Trim per-host Product tests to routing-only, consolidate scope tests into BaseProductControllerTests (ARCH-001 Phase 1)"
```

---

## Task 14: Full-solution verification

**Files:** none — verification only.

- [ ] **Step 1: Full solution build**

Run: `dotnet build GrandNode.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Full test run for the three web test projects**

Run:
```
dotnet test src/Tests/Grand.Web.Admin.Tests
dotnet test src/Tests/Grand.Web.Store.Tests
dotnet test src/Tests/Grand.Web.Vendor.Tests
```
Expected: all PASS. Per `project_test_suite_flaky_parallel` (project memory), run each project individually rather than via a single solution-wide `dotnet test` — the full-solution parallel run is known to flake on unrelated Customers/Marketing/Messages suites.

- [ ] **Step 3: Line-count sanity check against the ARCH-001 baseline**

Run: `wc -l src/Web/Grand.Web.Admin/Controllers/ProductController.cs src/Web/Grand.Web.Store/Controllers/ProductController.cs src/Web/Grand.Web.Vendor/Controllers/ProductController.cs src/Web/Grand.Web.AdminShared/Controllers/BaseProductController.cs`
Expected: the three host controllers are each roughly 20-40 lines (matching `LoginController`'s shape); `BaseProductController.cs` accounts for the bulk of what was previously ~2500 lines ×3.

- [ ] **Step 4: Manual smoke test (if a local Kestrel instance is available per `reference_running_the_storefront`)**

Log into each of the three admin panels and open Product → List → Edit → Save for one existing product per host, confirming no runtime DI resolution errors and that store/vendor scoping still restricts what's visible (a Store-scoped or Vendor-scoped login should not see or be able to open another store's/vendor's product).

- [ ] **Step 5: Update the ARCH-001 project memory**

Edit `project_arch001_triple_admin_duplication.md` (memory file) to note Phase 1 (Product controller + service) is complete, Phase 2 (views) is a separate follow-up plan, and that the pattern is now proven for future entities (Order, Category, Collection).

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "ARCH-001 Phase 1 complete: Product controller and service consolidated into AdminShared"
```
