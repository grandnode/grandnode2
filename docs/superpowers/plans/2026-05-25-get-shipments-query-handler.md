# GetShipmentsQuery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a CQRS query (`GetShipmentsQuery` + `GetShipmentsQueryHandler`) that exposes `IQueryable<Shipment>` with composable filtering, closing the gap where Orders and Returns have query handlers but Shipments do not.

**Architecture:** The query contract lives in `Grand.Business.Core` (public API); the handler lives in `Grand.Business.Checkout` (implementation). The handler injects `IRepository<Shipment>` directly and builds an `IQueryable<Shipment>` by chaining `Where` clauses, then returns it synchronously via `Task.FromResult` — identical to `GetOrderQueryHandler` and `GetMerchandiseReturnQueryHandler`.

**Tech Stack:** .NET 9, MediatR, MongoDB (`IRepository<T>`), MSTest, `MongoDBRepositoryTest<T>` for in-process test repository.

---

## File Map

| Action | Path |
|--------|------|
| **Create** | `src/Business/Grand.Business.Core/Queries/Checkout/Orders/GetShipmentsQuery.cs` |
| **Create** | `src/Business/Grand.Business.Checkout/Queries/Handlers/Orders/GetShipmentsQueryHandler.cs` |
| **Create** | `src/Tests/Grand.Business.Checkout.Tests/Queries/Handlers/Orders/GetShipmentsQueryHandlerTests.cs` |

---

## Task 1: Create the query contract

**Files:**
- Create: `src/Business/Grand.Business.Core/Queries/Checkout/Orders/GetShipmentsQuery.cs`

- [ ] **Step 1: Create the file**

```csharp
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetShipmentsQuery : IRequest<IQueryable<Shipment>>
{
    public string StoreId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string TrackingNumber { get; set; } = null;
    public bool LoadNotShipped { get; set; } = false;
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
}
```

- [ ] **Step 2: Build to confirm it compiles**

```powershell
dotnet build src/Business/Grand.Business.Core/Grand.Business.Core.csproj --configuration Release -v q
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 3: Commit**

```powershell
git add src/Business/Grand.Business.Core/Queries/Checkout/Orders/GetShipmentsQuery.cs
git commit -m "Add GetShipmentsQuery contract"
```

---

## Task 2: Write failing tests

**Files:**
- Create: `src/Tests/Grand.Business.Checkout.Tests/Queries/Handlers/Orders/GetShipmentsQueryHandlerTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetShipmentsQueryHandlerTests
{
    private GetShipmentsQueryHandler _handler;
    private MongoDBRepositoryTest<Shipment> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Shipment>();
        _repository.Insert(new Shipment { StoreId = "store1", VendorId = "vendor1", OrderId = "order1", TrackingNumber = "TN-001", ShippedDateUtc = DateTime.UtcNow, CreatedOnUtc = DateTime.UtcNow.AddDays(-3), ShipmentItems = new List<ShipmentItem> { new ShipmentItem { WarehouseId = "wh1" } } });
        _repository.Insert(new Shipment { StoreId = "store1", VendorId = "vendor2", OrderId = "order2", TrackingNumber = "TN-002", ShippedDateUtc = null,              CreatedOnUtc = DateTime.UtcNow.AddDays(-1), ShipmentItems = new List<ShipmentItem> { new ShipmentItem { WarehouseId = "wh2" } } });
        _repository.Insert(new Shipment { StoreId = "store2", VendorId = "vendor1", OrderId = "order3", TrackingNumber = "TN-003", ShippedDateUtc = DateTime.UtcNow, CreatedOnUtc = DateTime.UtcNow.AddDays(-2), ShipmentItems = new List<ShipmentItem> { new ShipmentItem { WarehouseId = "wh1" } } });
        _handler = new GetShipmentsQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Handle_NoFilters_ReturnsAllShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery(), CancellationToken.None);
        Assert.AreEqual(3, result.Count());
    }

    [TestMethod]
    public async Task Handle_FilterByStoreId_ReturnsMatchingShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { StoreId = "store1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.StoreId == "store1"));
    }

    [TestMethod]
    public async Task Handle_FilterByVendorId_ReturnsMatchingShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { VendorId = "vendor1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.VendorId == "vendor1"));
    }

    [TestMethod]
    public async Task Handle_FilterByOrderId_ReturnsSingleShipment()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { OrderId = "order1" }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("order1", result.First().OrderId);
    }

    [TestMethod]
    public async Task Handle_FilterByTrackingNumber_ReturnsSingleShipment()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { TrackingNumber = "TN-002" }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("TN-002", result.First().TrackingNumber);
    }

    [TestMethod]
    public async Task Handle_LoadNotShipped_ReturnsOnlyUnshippedShipments()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { LoadNotShipped = true }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.IsNull(result.First().ShippedDateUtc);
    }

    [TestMethod]
    public async Task Handle_FilterByWarehouseId_ReturnsShipmentsWithMatchingItems()
    {
        var result = await _handler.Handle(new GetShipmentsQuery { WarehouseId = "wh1" }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.ShipmentItems.Any(i => i.WarehouseId == "wh1")));
    }

    [TestMethod]
    public async Task Handle_FilterByCreatedFromUtc_ReturnsShipmentsAfterDate()
    {
        var cutoff = DateTime.UtcNow.AddDays(-2).AddHours(-1);
        var result = await _handler.Handle(new GetShipmentsQuery { CreatedFromUtc = cutoff }, CancellationToken.None);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(s => s.CreatedOnUtc >= cutoff));
    }

    [TestMethod]
    public async Task Handle_FilterByCreatedToUtc_ReturnsShipmentsBeforeDate()
    {
        var cutoff = DateTime.UtcNow.AddDays(-2).AddHours(1);
        var result = await _handler.Handle(new GetShipmentsQuery { CreatedToUtc = cutoff }, CancellationToken.None);
        Assert.AreEqual(1, result.Count());
        Assert.IsTrue(result.All(s => s.CreatedOnUtc <= cutoff));
    }

    [TestMethod]
    public async Task Handle_ResultIsOrderedByCreatedOnUtcDescending()
    {
        var result = (await _handler.Handle(new GetShipmentsQuery(), CancellationToken.None)).ToList();
        for (var i = 0; i < result.Count - 1; i++)
            Assert.IsTrue(result[i].CreatedOnUtc >= result[i + 1].CreatedOnUtc);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail** (handler does not exist yet)

```powershell
dotnet test src/Tests/Grand.Business.Checkout.Tests/Grand.Business.Checkout.Tests.csproj --filter "FullyQualifiedName~GetShipmentsQueryHandlerTests" --configuration Release
```

Expected: **compilation error** — `GetShipmentsQueryHandler` not found.

---

## Task 3: Implement the handler

**Files:**
- Create: `src/Business/Grand.Business.Checkout/Queries/Handlers/Orders/GetShipmentsQueryHandler.cs`

- [ ] **Step 1: Create the handler file**

```csharp
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetShipmentsQueryHandler : IRequestHandler<GetShipmentsQuery, IQueryable<Shipment>>
{
    private readonly IRepository<Shipment> _shipmentRepository;

    public GetShipmentsQueryHandler(IRepository<Shipment> shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public Task<IQueryable<Shipment>> Handle(GetShipmentsQuery request, CancellationToken cancellationToken)
    {
        var query = from s in _shipmentRepository.Table
            select s;

        if (!string.IsNullOrEmpty(request.StoreId))
            query = query.Where(s => s.StoreId == request.StoreId);

        if (!string.IsNullOrEmpty(request.VendorId))
            query = query.Where(s => s.VendorId == request.VendorId);

        if (!string.IsNullOrEmpty(request.WarehouseId))
            query = query.Where(s => s.ShipmentItems.Any(i => i.WarehouseId == request.WarehouseId));

        if (!string.IsNullOrEmpty(request.OrderId))
            query = query.Where(s => s.OrderId == request.OrderId);

        if (!string.IsNullOrEmpty(request.TrackingNumber))
            query = query.Where(s => s.TrackingNumber == request.TrackingNumber);

        if (request.LoadNotShipped)
            query = query.Where(s => s.ShippedDateUtc == null);

        if (request.CreatedFromUtc.HasValue)
            query = query.Where(s => s.CreatedOnUtc >= request.CreatedFromUtc.Value);

        if (request.CreatedToUtc.HasValue)
            query = query.Where(s => s.CreatedOnUtc <= request.CreatedToUtc.Value);

        query = query.OrderByDescending(s => s.CreatedOnUtc);

        return Task.FromResult(query);
    }
}
```

- [ ] **Step 2: Build the checkout project**

```powershell
dotnet build src/Business/Grand.Business.Checkout/Grand.Business.Checkout.csproj --configuration Release -v q
```

Expected: `Build succeeded.` with 0 errors.

---

## Task 4: Run tests and commit

- [ ] **Step 1: Run all GetShipments tests**

```powershell
dotnet test src/Tests/Grand.Business.Checkout.Tests/Grand.Business.Checkout.Tests.csproj --filter "FullyQualifiedName~GetShipmentsQueryHandlerTests" --configuration Release -v normal
```

Expected: **9 tests pass, 0 failures.**

- [ ] **Step 2: Run full checkout test suite to check for regressions**

```powershell
dotnet test src/Tests/Grand.Business.Checkout.Tests/Grand.Business.Checkout.Tests.csproj --configuration Release -v q
```

Expected: All tests pass, 0 failures.

- [ ] **Step 3: Commit all three files**

```powershell
git add src/Business/Grand.Business.Core/Queries/Checkout/Orders/GetShipmentsQuery.cs
git add src/Business/Grand.Business.Checkout/Queries/Handlers/Orders/GetShipmentsQueryHandler.cs
git add src/Tests/Grand.Business.Checkout.Tests/Queries/Handlers/Orders/GetShipmentsQueryHandlerTests.cs
git commit -m "Add GetShipmentsQuery and GetShipmentsQueryHandler"
```

---

## Self-Review Checklist (run before executing)

- [x] All 3 spec requirements covered: query contract, handler, tests
- [x] No TBDs or placeholders
- [x] Type names consistent across all tasks: `GetShipmentsQuery`, `GetShipmentsQueryHandler`
- [x] Property names match `Shipment` domain model: `StoreId`, `VendorId`, `OrderId`, `TrackingNumber`, `ShippedDateUtc`, `CreatedOnUtc`
- [x] `WarehouseId` filtered via `ShipmentItems` (no top-level field on `Shipment`)
- [x] Return type `IQueryable<Shipment>` consistent across query, handler, and test assertions
- [x] Test framework: MSTest (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`) — matches checkout test project
- [x] Test repository: `MongoDBRepositoryTest<Shipment>` — matches pattern in `GetCustomerQueryHandlerTests`
