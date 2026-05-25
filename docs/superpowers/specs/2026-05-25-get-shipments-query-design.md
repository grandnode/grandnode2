# GetShipmentsQuery — Design Spec

**Date:** 2026-05-25  
**Branch:** `feature/get-shipments-query-handler`  
**Status:** Approved

---

## Problem

Orders and MerchandiseReturns have CQRS query handlers (`GetOrderQuery`, `GetMerchandiseReturnQuery`) that expose `IQueryable<T>` for composable, pageable filtering. Shipments lack this — callers must go through `IShipmentService.GetAllShipments()` which returns a pre-paged `IPagedList<Shipment>` and cannot be composed further.

---

## Solution

Add two files following the established checkout query handler pattern:

1. `GetShipmentsQuery` — the MediatR request contract (in `Grand.Business.Core`)
2. `GetShipmentsQueryHandler` — the handler (in `Grand.Business.Checkout`)

---

## File Locations

| File | Path |
|------|------|
| Query | `src/Business/Grand.Business.Core/Queries/Checkout/Orders/GetShipmentsQuery.cs` |
| Handler | `src/Business/Grand.Business.Checkout/Queries/Handlers/Orders/GetShipmentsQueryHandler.cs` |

Both placed in `Orders/` namespace — consistent with all other checkout query handlers.

---

## Query Contract

```csharp
namespace Grand.Business.Core.Queries.Checkout.Orders;

public class GetShipmentsQuery : IRequest<IQueryable<Shipment>>
{
    public string StoreId        { get; set; } = "";
    public string VendorId       { get; set; } = "";
    public string WarehouseId    { get; set; } = "";
    public string OrderId        { get; set; } = "";
    public string TrackingNumber { get; set; } = null;
    public bool LoadNotShipped   { get; set; } = false;
    public DateTime? CreatedFromUtc { get; set; } = null;
    public DateTime? CreatedToUtc   { get; set; } = null;
    public int PageIndex { get; set; } = 0;
    public int PageSize  { get; set; } = int.MaxValue;
}
```

---

## Handler Design

- **Dependency:** `IRepository<Shipment>` (direct repository, no service)
- **Pattern:** builds `IQueryable<Shipment>` from `_repository.Table`, applies `Where` clauses for each non-empty filter, returns `Task.FromResult(query)`
- **Ordering:** `OrderByDescending(s => s.CreatedOnUtc)` — consistent with Orders and Returns

### Filter logic

| Property | Shipment field | Condition |
|----------|---------------|-----------|
| `StoreId` | `s.StoreId` | `!string.IsNullOrEmpty` |
| `VendorId` | `s.VendorId` | `!string.IsNullOrEmpty` |
| `WarehouseId` | `s.ShipmentItems.Any(i => i.WarehouseId == ...)` | `!string.IsNullOrEmpty` |
| `OrderId` | `s.OrderId` | `!string.IsNullOrEmpty` |
| `TrackingNumber` | `s.TrackingNumber` | `!string.IsNullOrEmpty` |
| `LoadNotShipped` | `s.ShippedDateUtc == null` | `== true` |
| `CreatedFromUtc` | `s.CreatedOnUtc >= value` | `.HasValue` |
| `CreatedToUtc` | `s.CreatedOnUtc <= value` | `.HasValue` |

> `PageIndex` and `PageSize` are carried on the query for callers that want to page — the handler returns the full `IQueryable` and leaves paging to the caller.

---

## Design Decisions

- **No `IShipmentService` dependency** — direct `IRepository<Shipment>` access matches how `GetOrderQueryHandler` and `GetMerchandiseReturnQueryHandler` work. The service layer wraps these handlers; inverting that would create a circular dependency.
- **`IQueryable<Shipment>` return type** — composable; callers can add further projections or paging without re-querying the database.
- **`WarehouseId` via `ShipmentItems`** — the `Shipment` entity has no top-level `WarehouseId`; it lives on each `ShipmentItem`, mirroring how `GetOrderQueryHandler` handles warehouse filtering via `OrderItems`.

---

## Out of Scope

- No changes to `IShipmentService` or `ShipmentService`
- No new API endpoints (separate concern)
- No admin/vendor UI changes
