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
