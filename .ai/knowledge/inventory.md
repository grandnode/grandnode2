# Inventory

How physical stock changes, and the rules that keep the stored counters equal to the sum of the inventory journal. Introduced in PR #1036.

---

## Where stock lives

The counters stay on the product document. Which ones are the stock depends on `ManageInventoryMethodId` and `UseMultipleWarehouses`. Each such place is a **stock bucket**:

| Method | Bucket |
|---|---|
| `ManageStock`, one warehouse | `Product.StockQuantity` |
| `ManageStock`, multiple warehouses | a `Product.ProductWarehouseInventory` row |
| `ManageStockByAttributes` | `ProductAttributeCombination.StockQuantity`, or a row of its `WarehouseInventory` |
| `ManageStockByBundleProducts`, `DontManageStock` | none: not tracked, plain counters |

With multiple warehouses or combinations, the product and combination counters are **sums** of their buckets. `StockBuckets` (`Grand.Business.Core/Utilities/Catalog`) enumerates the buckets and holds the shared query filters (`KeptIn(warehouseId)`).

## The one path

Every change of tracked physical stock goes through `IStockMovementService.Apply(productId, changes)`. A `StockChange` names the bucket (attributes, warehouse), a signed difference or a target (`SetTo`), an `InventoryMovementType` and a `StockSource` (object type, id, line, reference, customer, comment).

Under `ProductStockLock.Key(productId)` (`IResourceLock`), `StockLedger` reloads the product, writes one `InventoryJournal` entry per change, then writes all stock fields of the product in one update stamped with `Product.StockRevision`. An entry newer than the product's revision is one whose product write was lost; it is re-applied before the next change. Side effects (low-stock activity, the "quantity below" e-mail, back-in-stock notifications) run in `StockSideEffects`, once, after the write.

Callers:

- back-office forms and grids post the `Original*` value next to the edited one; only a changed field becomes a `SetTo` (`ManualEdit`);
- API (`Api`), product import (`Import`), product copy (`ProductCopy`), new products (`JournalNewProduct`);
- shipments through `IInventoryManageService.BookReservedInventory` / `ReverseBookedInventory` (`Shipment`, `ShipmentReversal`);
- inventory documents through `IInventoryDocumentService.Post`.

Reservations are order state, not journaled: `IInventoryManageService.TryReserve` / `CancelReservation` change `ReservedQuantity` under the same lock key.

## Rules

- **The lock is not re-entrant.** Code that already holds `ProductStockLock.Key` calls `StockLedger` directly, never `IStockMovementService`, which would wait for its own lock.
- **The journal is append-only.** Nothing edits or deletes an `InventoryJournal` entry. A shipment reversal writes `ShipmentReversal`; rows from before 2.5 are `Legacy`.
- **`ProductService` does not write tracked stock.** `UpdateProduct`, `UpdateProductAttributeCombination` and the warehouse-row methods leave the stock fields of a tracked product alone.
- **A structural change re-opens the buckets.** A change of inventory method, `UseMultipleWarehouses` or the single warehouse (also on warehouse delete) goes through `StockLedger.Reopen`: a `ClosingBalance` per non-zero old bucket, then an `OpeningBalance` per current bucket. Opening, closing and legacy entries record a balance; they never move stock.
- **`InventorySettings` is system-wide.** It is stored with an empty store id and never overridden per store. With `StockChangesOnlyThroughDocuments` on, the back-office stock fields are read-only and the server rejects a changed value; the API still works.

## Inventory documents

`InventoryDocument` with embedded `InventoryDocumentLine`s, of type `Receipt`, `Transfer`, `Adjustment`, `StockCount` or `ReturnReceipt`. A draft is editable. `Post` validates every line before the first write, books the lines and freezes the document; a mistake is fixed by a new document. `Post`, `UpdateDraft` and `DeleteDraft` hold `InventoryDocument:{id}`, and `Post` takes the product keys one product at a time.

- A receipt updates the weighted average `Product.ProductCost`, 4 decimals (the product form field is `DoubleN4`).
- A stock count freezes `ExpectedQuantity` at creation and books counted − expected against the current stock.
- A return receipt is created from a merchandise return; `Scrap` lines change no stock.
- At most 5000 lines a document; the edit form pages 100 lines (the 1024 form-field limit).

Admin and Store share `BaseInventoryDocumentController`. The Store panel uses only its own store's warehouses and products; shared warehouses (empty `StoreId`) are Main Admin-only on documents. Permission: `PermissionSystemName.InventoryDocuments`.

## Batches and serial numbers

A product of `ManageStock` or `ManageStockByAttributes` tracks units by `Product.InventoryTrackingModeId`: `None`, `Batch` or `SerialNumber`, never both. `InventoryTracking.Tracked(product)` is the test; mode `None` behaves exactly as without this feature.

- **`ProductBatch`**: one record per product, combination, bucket warehouse and batch number, with expiry date, unit cost and quantity. A record at 0 stays, for recalls.
- **`ProductSerialNumber`**: one record per unit, `InStock`, `Shipped`, `Scrapped` or `Missing`, with its warehouse and, once shipped, order, shipment and `WarrantyUntilUtc`.
- **Pool**: not stored. A bucket's stock less its assigned units (batch quantities, or in-stock serial numbers). Switching tracking on makes the whole stock the pool (`TrackingStarted` marker); an `Assignment` document, a shipment naming a new serial number, or a stock count fills it.
- Numbers compare by `NumberKey` (`UnitNumbers.Key`: trimmed, upper-cased); both unique indexes use it.
- The journal names a unit as its record spells it (`StockUnits.Name`, before the entries are inserted), so a unit's history and a batch's recipients filter by number in the database, scoped to the batch's warehouse and combination, and read a page at a time. Never load a product's journal to filter it in memory.
- Units live in the bucket's journal warehouse: `StockBuckets.UnitWarehouse(product, warehouseId)`. A single-warehouse product keeps them in its own warehouse whatever an order item names.

### How units move

A `StockChange` names its units (`BatchNumber`, `ExpiryDateUtc`, `SerialNumbers`, or `CountedSerialNumbers` for a counted serial line); the rest of its quantity is the pool. `StockLedger` hands them to `StockUnits` under the same product lock: entries are written first, then the unit records, then the product. Each unit record carries the `StockRevision` of the last entry that changed it, so the repair re-applies a lost unit write once and never twice (`RepairWithUnits`, also in `Reopen`, `RecomputeSums` and `StartTracking`).

- `TrackingAssignment` (`InQty` = `OutQty` = n) and `ReturnScrapped` (0/0) change units, never stock.
- A serial-mode `Shipment` names exactly its quantity of numbers; a batch pool goes below zero only through a shipment of a product allowing backorders.
- A counted serial line is declarative ("these are all the units of the bucket") and is expanded under the lock; posting it again books only what still differs.
- Shipments: `IProductUnitService.PrepareShipmentUnits` checks the units and stamps the warranty before the shipment is stored; `BookReservedInventory` books them, and a unit taken meanwhile leaves the item `UnitsNotBooked` with nothing booked. Bundle children book from the pool; a serial-tracked child refuses the bundle item.
- Stock of a tracked product changes only through documents and shipments (`InventoryTracking.StockDocumentOnly`): forms, bulk edit, import and the API refuse a changed value.
- A change of mode (other than from `None`), inventory method or warehouses, and deleting a combination or warehouse, is refused while units are assigned (`IProductUnitService.TrackingChangeRefused`, `AssignedQuantity`).

### What the back office tells about a unit

A batch or serial number also carries optional details the ledger never writes: a `LocationId` (a `WarehouseLocation`, a place in the record's warehouse, managed under Configuration > Shipping), the supplier, dates, device fields (IMEI, MAC address, firmware) and two comments, one for the admin and one shown to the customer on the shipment page. `IProductUnitService.UpdateBatchDetails` / `UpdateSerialNumberDetails` write only those fields, without the product's lock, so `StockUnits.Write` writes only the ledger's fields of a changed record, never the whole record. A batch new in a warehouse takes the details of the same batch elsewhere, never its location; a serial number moved to another warehouse keeps a location that no longer applies, which every screen ignores. The batch and serial number pages are `<admin-tabstrip>`s (`productbatch-edit`, `productserialnumber-edit`) a plugin extends through `AdminTabStripCreated`.

Screens: `BaseProductUnitController` (serial numbers, batches, recall CSV), the product's units tab, the order views (`ShippedUnits.Of` reads the shipments). The daily task "Inventory batch expiry notification" e-mails `Inventory.BatchesExpiring.StoreOwnerNotification` per store, once per status (`ProductBatch.ExpiryNotifiedStatusId`). Expiry never blocks.
