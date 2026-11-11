using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using System.Globalization;

namespace Grand.Business.Catalog.Services.Inventory;

public class InventoryDocumentService(
    IRepository<InventoryDocument> inventoryDocumentRepository,
    IRepository<Product> productRepository,
    IRepository<InventoryJournal> inventoryJournalRepository,
    IWarehouseService warehouseService,
    IResourceLock resourceLock,
    ISettingService settingService,
    IMediator mediator,
    StockLedger stockLedger,
    IMerchandiseReturnService merchandiseReturnService,
    IOrderService orderService,
    IProductUnitService productUnitService,
    IShipmentService shipmentService) : IInventoryDocumentService
{
    private const int StockCountPageSize = 500;

    /// <summary>
    ///     The lines a document holds at most: they are stored with it, and a large warehouse is counted in parts
    /// </summary>
    public const int MaxLines = 5000;

    public async Task<(InventoryDocument Document, IList<string> Errors)> CreateStockCount(string warehouseId,
        IList<string> categoryIds, IList<string> brandIds, string customerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(warehouseId);

        var query = productRepository.Table.Where(StockBuckets.KeptIn(warehouseId));
        if (categoryIds is { Count: > 0 })
            query = query.Where(x => x.ProductCategories.Any(c => categoryIds.Contains(c.CategoryId)));
        if (brandIds is { Count: > 0 })
            query = query.Where(x => brandIds.Contains(x.BrandId));
        query = query.OrderBy(x => x.Id);

        var document = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.StockCount,
            WarehouseId = warehouseId,
            CreatedBy = customerId
        };
        for (var pageIndex = 0;; pageIndex++)
        {
            var products = await productRepository.PagedAsync(query, pageIndex, StockCountPageSize);
            foreach (var product in products)
            foreach (var (combinationId, attributes, stock) in StockBuckets.In(product, warehouseId))
                await AddStockCountLines(document, product, combinationId, attributes, stock);

            if (document.Lines.Count > MaxLines)
                return (null, [InventoryDocumentErrors.TooManyLines]);
            if (!products.HasNextPage)
                break;
        }

        return (await CreateDraft(document), []);
    }

    /// <summary>
    ///     The lines a bucket is counted on: one, or for a batch-tracked product one per batch holding stock and one
    ///     for the stock without a batch. A serial-tracked bucket is one line whose serial numbers are scanned
    /// </summary>
    private async Task AddStockCountLines(InventoryDocument document, Product product, string combinationId,
        IList<CustomAttribute> attributes, int stock)
    {
        if (!InventoryTracking.Tracked(product) || product.InventoryTrackingModeId != InventoryTrackingMode.Batch)
        {
            document.Lines.Add(new InventoryDocumentLine
                { ProductId = product.Id, Attributes = attributes, ExpectedQuantity = stock });
            return;
        }

        var batches = await productUnitService.Batches(product.Id, combinationId,
            StockBuckets.UnitWarehouse(product, document.WarehouseId));
        foreach (var batch in batches)
            document.Lines.Add(new InventoryDocumentLine {
                ProductId = product.Id, Attributes = attributes, BatchNumber = batch.Number,
                ExpectedQuantity = batch.Quantity
            });
        document.Lines.Add(new InventoryDocumentLine {
            ProductId = product.Id, Attributes = attributes, ExpectedQuantity = stock - batches.Sum(x => x.Quantity)
        });
    }

    /// <summary>
    ///     What the shipments of an order shipped of each order item: the quantity, the serial numbers and batches
    /// </summary>
    private async Task<Dictionary<string, (int Quantity, List<string> SerialNumbers, List<ShipmentItemBatch> Batches)>>
        Shipped(string orderId)
    {
        var shipped = new Dictionary<string, (int, List<string>, List<ShipmentItemBatch>)>();
        foreach (var item in (await shipmentService.GetShipmentsByOrder(orderId)).SelectMany(x => x.ShipmentItems))
        {
            var (quantity, serialNumbers, batches) = shipped.GetValueOrDefault(item.OrderItemId,
                (0, new List<string>(), new List<ShipmentItemBatch>()));
            serialNumbers.AddRange(item.SerialNumbers ?? []);
            batches.AddRange(item.Batches ?? []);
            shipped[item.OrderItemId] = (quantity + item.Quantity, serialNumbers, batches);
        }

        return shipped;
    }

    public async Task<(InventoryDocument Document, IList<string> Errors)> CreateReturnReceipt(
        string merchandiseReturnId, string customerId)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return (null, [InventoryDocumentErrors.NotFound]);
        if (!merchandiseReturn.MerchandiseReturnStatus.TakesQuantity())
            return (null, [InventoryDocumentErrors.ReturnNotReceivable]);

        var returned = await Returned(merchandiseReturn);
        var received = await Received(merchandiseReturn.Id, null);
        var document = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.ReturnReceipt,
            MerchandiseReturnId = merchandiseReturn.Id,
            CreatedBy = customerId
        };
        Dictionary<string, (int Quantity, List<string> SerialNumbers, List<ShipmentItemBatch> Batches)> shipped = null;
        foreach (var item in returned)
        {
            var quantity = item.Quantity - received.GetValueOrDefault((item.ReturnItemId, item.ProductId));
            if (quantity <= 0)
                continue;

            var line = new InventoryDocumentLine {
                ProductId = item.ProductId, Attributes = item.Attributes, Quantity = quantity,
                MerchandiseReturnItemId = item.ReturnItemId
            };
            //the units shipped for the order item come back: the serial numbers to untick, or the batches
            var product = await productRepository.GetByIdAsync(item.ProductId);
            var orderItemId = merchandiseReturn.MerchandiseReturnItems.FirstOrDefault(x => x.Id == item.ReturnItemId)
                ?.OrderItemId;
            if (product != null && InventoryTracking.Tracked(product) && orderItemId != null &&
                (shipped ??= await Shipped(merchandiseReturn.OrderId)).TryGetValue(orderItemId, out var units))
            {
                if (product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber)
                    line.SerialNumbers = units.SerialNumbers.ToList();
                else
                    foreach (var batch in units.Batches.GroupBy(x => UnitNumbers.Key(x.Number)))
                    {
                        var fromBatch = Math.Min(line.Quantity, batch.Sum(x => x.Quantity));
                        if (fromBatch <= 0)
                            continue;
                        document.Lines.Add(new InventoryDocumentLine {
                            ProductId = item.ProductId, Attributes = item.Attributes, Quantity = fromBatch,
                            BatchNumber = batch.First().Number, MerchandiseReturnItemId = item.ReturnItemId
                        });
                        line.Quantity -= fromBatch;
                    }
            }

            if (line.Quantity > 0)
                document.Lines.Add(line);
        }

        if (document.Lines.Count == 0)
            return (null, [InventoryDocumentErrors.NoLines]);

        //the warehouse the items were sold from, when it is one
        var warehouses = returned.Select(x => x.WarehouseId ?? "").Distinct().ToList();
        document.WarehouseId = warehouses.Count == 1 ? warehouses[0] : "";

        return (await CreateDraft(document), []);
    }

    /// <summary>
    ///     What a merchandise return gives back, by product: a returned bundle gives back its products
    /// </summary>
    private async Task<IList<(string ReturnItemId, string ProductId, IList<CustomAttribute> Attributes, int Quantity,
            string WarehouseId)>>
        Returned(MerchandiseReturn merchandiseReturn)
    {
        var returned = new List<(string, string, IList<CustomAttribute>, int, string)>();
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        if (order == null)
            return returned;

        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            if (orderItem == null)
                continue;

            var product = await productRepository.GetByIdAsync(orderItem.ProductId);
            if (product?.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
                returned.AddRange(product.BundleProducts.Select(child =>
                    (item.Id, child.ProductId, (IList<CustomAttribute>)[], item.Quantity * child.Quantity,
                        orderItem.WarehouseId)));
            else
                returned.Add((item.Id, orderItem.ProductId, orderItem.Attributes ?? [], item.Quantity,
                    orderItem.WarehouseId));
        }

        return returned;
    }

    /// <summary>
    ///     The quantities the return receipts of a merchandise return take in, posted or draft, by return item and
    ///     product
    /// </summary>
    /// <param name="merchandiseReturnId">The merchandise return</param>
    /// <param name="exceptDocumentId">A document left out</param>
    private async Task<Dictionary<(string, string), int>> Received(string merchandiseReturnId,
        string exceptDocumentId)
    {
        var receipts = await inventoryDocumentRepository.ToListAsync(inventoryDocumentRepository.Table.Where(x =>
            x.DocumentTypeId == InventoryDocumentType.ReturnReceipt && x.MerchandiseReturnId == merchandiseReturnId &&
            x.Id != exceptDocumentId));
        return receipts.SelectMany(x => x.Lines)
            .GroupBy(x => (x.MerchandiseReturnItemId, x.ProductId))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
    }

    public async Task<IList<string>> Post(string id, string customerId)
    {
        //every line is checked before the first write
        InventoryDocument document;
        await using (await resourceLock.Acquire([DocumentLockKey(id)]))
        {
            document = await inventoryDocumentRepository.GetByIdAsync(id);
            if (document == null)
                return [InventoryDocumentErrors.NotFound];
            if (document.StatusId == InventoryDocumentStatus.Posted)
                return [InventoryDocumentErrors.Posted];

            var invalid = await Validate(document);
            if (invalid.Count > 0)
                return invalid;
        }

        //one product at a time, with the document's key: no lease has to outlast a large stock count, and a draft
        //saved meanwhile stops the posting instead of being booked half as it was; a product that fails stops it
        //too, and posting again continues after the products booked
        foreach (var productLines in document.Lines.GroupBy(x => x.ProductId))
        {
            await using var _ = await resourceLock.Acquire([DocumentLockKey(id), ProductStockLock.Key(productLines.Key)]);
            if (await Changed(document))
                return [InventoryDocumentErrors.ChangedWhilePosting];

            var product = await productRepository.GetByIdAsync(productLines.Key);
            var booked = await inventoryJournalRepository.ToListAsync(inventoryJournalRepository.Table
                .Where(x => x.ObjectType == nameof(InventoryDocument) && x.ObjectId == document.Id &&
                            x.ProductId == productLines.Key)
                .Select(x => new { x.PositionId, x.MovementTypeId }));

            //a product's changes are booked together, so a product with entries of the document is done: a counted
            //serial line, worked out against the units as they are now, would count again what changed since
            var changes = productLines.SelectMany(line => Changes(document, line, customerId, product))
                .Where(change => change.CountedSerialNumbers != null
                    ? booked.Count == 0
                    : !booked.Any(x =>
                        x.PositionId == change.Source.PositionId && x.MovementTypeId == change.MovementType))
                .ToList();
            if (changes.Count == 0)
                continue;

            var errors = await stockLedger.Apply(product, changes);
            if (errors.Count > 0)
                return errors;
        }

        await using (await resourceLock.Acquire([DocumentLockKey(id)]))
        {
            if (await Changed(document))
                return [InventoryDocumentErrors.ChangedWhilePosting];

            document.StatusId = InventoryDocumentStatus.Posted;
            document.PostedOnUtc = DateTime.UtcNow;
            document.PostedBy = customerId;
            await inventoryDocumentRepository.UpdateOneAsync(x => x.Id == document.Id,
                UpdateBuilder<InventoryDocument>.Create()
                    .Set(x => x.StatusId, document.StatusId)
                    .Set(x => x.PostedOnUtc, document.PostedOnUtc)
                    .Set(x => x.PostedBy, document.PostedBy));
        }

        await mediator.EntityUpdated(document);
        return [];
    }

    /// <summary>
    ///     Whether the stored document is no longer the draft the posting read: saved, deleted or posted since
    /// </summary>
    private async Task<bool> Changed(InventoryDocument document)
    {
        var stored = await inventoryDocumentRepository.FirstOrDefaultAsync(inventoryDocumentRepository.Table
            .Where(x => x.Id == document.Id)
            .Select(x => new { x.StatusId, x.UpdatedOnUtc }));
        return stored == null || stored.StatusId != InventoryDocumentStatus.Draft ||
               stored.UpdatedOnUtc != document.UpdatedOnUtc;
    }

    /// <summary>
    ///     The stock changes a line books
    /// </summary>
    private static IEnumerable<StockChange> Changes(InventoryDocument document, InventoryDocumentLine line,
        string customerId, Product product)
    {
        var serialTracked = product != null && InventoryTracking.Tracked(product) &&
                            product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber;
        var source = new StockSource(nameof(InventoryDocument), document.Id, line.Id, document.Number, customerId,
            line.Comment ?? document.Comment ?? "");
        //the units a line names; the ledger books them for a product tracking them and ignores them otherwise
        StockChange Change(string warehouseId, int quantity, InventoryMovementType movementType) =>
            new(line.Attributes, warehouseId, quantity, false, movementType, source) {
                BatchNumber = string.IsNullOrWhiteSpace(line.BatchNumber) ? null : line.BatchNumber.Trim(),
                ExpiryDateUtc = line.ExpiryDateUtc,
                SerialNumbers = (line.SerialNumbers ?? []).ToList()
            };

        switch (document.DocumentTypeId)
        {
            case InventoryDocumentType.Receipt:
                yield return Change(document.WarehouseId, line.Quantity, InventoryMovementType.Receipt) with {
                    UnitCost = line.UnitCost
                };
                break;
            case InventoryDocumentType.Transfer:
                yield return Change(document.WarehouseId, -line.Quantity, InventoryMovementType.TransferOut);
                yield return Change(document.TargetWarehouseId, line.Quantity, InventoryMovementType.TransferIn);
                break;
            case InventoryDocumentType.Adjustment:
                yield return Change(document.WarehouseId, line.Quantity, InventoryMovementType.Adjustment);
                break;
            case InventoryDocumentType.StockCount when serialTracked:
                //the serial numbers scanned are all the units of the bucket
                if (line.CountedQuantity.HasValue)
                    yield return Change(document.WarehouseId, 0, InventoryMovementType.StockCount) with {
                        SerialNumbers = [], CountedSerialNumbers = (line.SerialNumbers ?? []).ToList()
                    };
                break;
            case InventoryDocumentType.StockCount:
                //the difference to what was expected, applied to the stock as it is now: a sale during the count
                //stays booked
                if (line.CountedQuantity.HasValue && line.CountedQuantity != line.ExpectedQuantity)
                    yield return Change(document.WarehouseId,
                        line.CountedQuantity.Value - (line.ExpectedQuantity ?? 0), InventoryMovementType.StockCount);
                break;
            case InventoryDocumentType.ReturnReceipt:
                //a scrapped item is recorded on the document only
                if (line.Condition == ReturnLineCondition.Restock)
                    yield return Change(document.WarehouseId, line.Quantity, InventoryMovementType.ReturnReceipt);
                //a scrapped unit with a serial number is marked so; it was out of stock already
                else if (serialTracked && line.SerialNumbers is { Count: > 0 })
                    yield return Change(document.WarehouseId, 0, InventoryMovementType.ReturnScrapped);
                break;
            case InventoryDocumentType.Assignment:
                yield return Change(document.WarehouseId, line.Quantity, InventoryMovementType.TrackingAssignment)
                    with { UnitCost = line.UnitCost };
                break;
        }
    }

    public Task<InventoryDocument> GetById(string id)
    {
        return inventoryDocumentRepository.GetByIdAsync(id);
    }

    public Task<IPagedList<InventoryDocument>> Search(InventoryDocumentSearch search, int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(search);

        var query = inventoryDocumentRepository.Table;
        if (search.Type.HasValue)
            query = query.Where(x => x.DocumentTypeId == search.Type.Value);
        if (search.Status.HasValue)
            query = query.Where(x => x.StatusId == search.Status.Value);
        if (!string.IsNullOrEmpty(search.WarehouseId))
            query = query.Where(x => x.WarehouseId == search.WarehouseId || x.TargetWarehouseId == search.WarehouseId);
        if (search.StoreId != null)
            query = query.Where(x => x.StoreId == search.StoreId);
        if (!string.IsNullOrEmpty(search.Number))
            query = query.Where(x => x.Number.Contains(search.Number));
        if (!string.IsNullOrEmpty(search.MerchandiseReturnId))
            query = query.Where(x => x.MerchandiseReturnId == search.MerchandiseReturnId);
        if (search.FromUtc.HasValue)
            query = query.Where(x => x.CreatedOnUtc >= search.FromUtc.Value);
        if (search.ToUtc.HasValue)
            query = query.Where(x => x.CreatedOnUtc <= search.ToUtc.Value);

        return inventoryDocumentRepository.PagedAsync(query.OrderByDescending(x => x.CreatedOnUtc), pageIndex,
            pageSize);
    }

    public async Task<InventoryDocument> CreateDraft(InventoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.StatusId = InventoryDocumentStatus.Draft;
        document.PostedOnUtc = null;
        document.PostedBy = null;
        document.StoreId = await StoreOf(document.WarehouseId);

        //numbers go per type and year; taking the next one and saving it hold the same lock, so no two drafts
        //share one, and a deleted draft leaves a gap
        var year = DateTime.UtcNow.Year;
        await using (await resourceLock.Acquire([$"InventoryDocumentNumber:{document.DocumentTypeId}:{year}"]))
        {
            var last = await inventoryDocumentRepository.FirstOrDefaultAsync(inventoryDocumentRepository.Table
                .Where(x => x.DocumentTypeId == document.DocumentTypeId && x.Year == year)
                .OrderByDescending(x => x.Sequence));

            document.Year = year;
            document.Sequence = (last?.Sequence ?? 0) + 1;
            document.Number = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2:0000}",
                await Prefix(document.DocumentTypeId), year, document.Sequence);
            await inventoryDocumentRepository.InsertAsync(document);
        }

        await mediator.EntityInserted(document);
        return document;
    }

    public async Task<IList<string>> UpdateDraft(InventoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        await using var documentLock = await resourceLock.Acquire([DocumentLockKey(document.Id)]);

        var stored = await inventoryDocumentRepository.GetByIdAsync(document.Id);
        if (stored == null)
            return [InventoryDocumentErrors.NotFound];
        if (stored.StatusId == InventoryDocumentStatus.Posted)
            return [InventoryDocumentErrors.Posted];

        //a posting that stopped part way booked some lines: those, and the warehouses they were booked in, stay
        var booked = await BookedLines(stored.Id);
        if (booked.Count > 0 && (document.WarehouseId != stored.WarehouseId ||
                                 document.TargetWarehouseId != stored.TargetWarehouseId ||
                                 stored.Lines.Where(x => booked.Contains(x.Id)).Any(x =>
                                     !SameBooking(x, document.Lines.FirstOrDefault(y => y.Id == x.Id)))))
            return [InventoryDocumentErrors.LineBooked];

        document.Number = stored.Number;
        document.Year = stored.Year;
        document.Sequence = stored.Sequence;
        document.DocumentTypeId = stored.DocumentTypeId;
        //a stock count's lines expect the stock of the warehouse it was made for
        if (stored.DocumentTypeId == InventoryDocumentType.StockCount)
            document.WarehouseId = stored.WarehouseId;
        document.StatusId = InventoryDocumentStatus.Draft;
        document.StoreId = await StoreOf(document.WarehouseId);
        await inventoryDocumentRepository.UpdateAsync(document);

        await mediator.EntityUpdated(document);
        return [];
    }

    public async Task<IList<string>> DeleteDraft(string id)
    {
        await using var documentLock = await resourceLock.Acquire([DocumentLockKey(id)]);

        var stored = await inventoryDocumentRepository.GetByIdAsync(id);
        if (stored == null)
            return [InventoryDocumentErrors.NotFound];
        if (stored.StatusId == InventoryDocumentStatus.Posted)
            return [InventoryDocumentErrors.Posted];
        if ((await BookedLines(stored.Id)).Count > 0)
            return [InventoryDocumentErrors.LineBooked];

        await inventoryDocumentRepository.DeleteAsync(stored);

        await mediator.EntityDeleted(stored);
        return [];
    }

    public async Task<IList<string>> Validate(InventoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var errors = new List<string>();

        void Add(string error)
        {
            if (!errors.Contains(error))
                errors.Add(error);
        }

        if (document.Lines.Count == 0)
            Add(InventoryDocumentErrors.NoLines);
        if (document.Lines.Count > MaxLines)
            Add(InventoryDocumentErrors.TooManyLines);

        if (string.IsNullOrEmpty(document.WarehouseId) ||
            await warehouseService.GetWarehouseById(document.WarehouseId) == null)
            Add(InventoryDocumentErrors.WarehouseNotFound);

        var transfer = document.DocumentTypeId == InventoryDocumentType.Transfer;
        if (transfer && (string.IsNullOrEmpty(document.TargetWarehouseId) ||
                         document.TargetWarehouseId == document.WarehouseId ||
                         await warehouseService.GetWarehouseById(document.TargetWarehouseId) == null))
            Add(InventoryDocumentErrors.TargetWarehouseInvalid);

        if (document.DocumentTypeId == InventoryDocumentType.Adjustment && string.IsNullOrWhiteSpace(document.Comment))
            Add(InventoryDocumentErrors.CommentRequired);

        //a stock count books the difference of each line: one bucket counted twice would book it twice
        if (document.DocumentTypeId == InventoryDocumentType.StockCount &&
            document.Lines.GroupBy(x => (x.ProductId, Attributes: AttributesKey(x.Attributes),
                Batch: UnitNumbers.Key(x.BatchNumber))).Any(x => x.Count() > 1))
            Add(InventoryDocumentErrors.DuplicateLine);

        //a return rejected, cancelled or deleted since the receipt was drafted gave its quantity back to the customer
        if (document.DocumentTypeId == InventoryDocumentType.ReturnReceipt &&
            !string.IsNullOrEmpty(document.MerchandiseReturnId) &&
            (await merchandiseReturnService.GetMerchandiseReturnById(document.MerchandiseReturnId))
            ?.MerchandiseReturnStatus.TakesQuantity() != true)
            Add(InventoryDocumentErrors.ReturnNotReceivable);

        if (document.DocumentTypeId == InventoryDocumentType.ReturnReceipt &&
            !string.IsNullOrEmpty(document.MerchandiseReturnId) &&
            await ReturnQuantityExceeded(document))
            Add(InventoryDocumentErrors.ReturnQuantityExceeded);

        var products = new Dictionary<string, Product>();
        foreach (var line in document.Lines)
        {
            Product product = null;
            if (!string.IsNullOrEmpty(line.ProductId) && !products.TryGetValue(line.ProductId, out product))
                products[line.ProductId] = product = await productRepository.GetByIdAsync(line.ProductId);
            if (product == null)
            {
                Add(StockErrors.ProductNotFound);
                continue;
            }

            if (product.ManageInventoryMethodId is not (ManageInventoryMethod.ManageStock
                or ManageInventoryMethod.ManageStockByAttributes))
            {
                Add(StockErrors.StockNotTracked);
                continue;
            }

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes &&
                (line.Attributes is not { Count: > 0 } ||
                 product.FindProductAttributeCombination(line.Attributes) == null))
                Add(StockErrors.CombinationNotFound);

            //a product kept in one warehouse is booked there, or anywhere when it names none
            if (!product.UseMultipleWarehouses && !string.IsNullOrEmpty(product.WarehouseId) &&
                product.WarehouseId != document.WarehouseId)
                Add(InventoryDocumentErrors.ProductInOtherWarehouse);

            if (transfer && !product.UseMultipleWarehouses)
                Add(InventoryDocumentErrors.TransferNeedsMultipleWarehouses);

            if (document.DocumentTypeId == InventoryDocumentType.Assignment && !InventoryTracking.Tracked(product))
                Add(InventoryDocumentErrors.UnitsNotTracked);
            foreach (var error in UnitErrorsOf(document, line, product))
                Add(error);

            switch (document.DocumentTypeId)
            {
                case InventoryDocumentType.Receipt:
                    if (line.Quantity <= 0) Add(InventoryDocumentErrors.QuantityMustBePositive);
                    if (line.UnitCost is null or < 0) Add(InventoryDocumentErrors.UnitCostRequired);
                    break;
                case InventoryDocumentType.Transfer:
                case InventoryDocumentType.ReturnReceipt:
                case InventoryDocumentType.Assignment:
                    if (line.Quantity <= 0) Add(InventoryDocumentErrors.QuantityMustBePositive);
                    break;
                case InventoryDocumentType.Adjustment:
                    if (line.Quantity == 0) Add(InventoryDocumentErrors.QuantityMustNotBeZero);
                    break;
                case InventoryDocumentType.StockCount:
                    if (line.CountedQuantity < 0) Add(InventoryDocumentErrors.CountedNegative);
                    //a surplus belongs to a batch: the stock without one can only be found missing
                    if (InventoryTracking.Tracked(product) &&
                        product.InventoryTrackingModeId == InventoryTrackingMode.Batch &&
                        string.IsNullOrWhiteSpace(line.BatchNumber) && line.CountedQuantity > line.ExpectedQuantity)
                        Add(UnitErrors.PoolExceeded);
                    break;
            }
        }

        if (document.DocumentTypeId == InventoryDocumentType.ReturnReceipt &&
            !string.IsNullOrEmpty(document.MerchandiseReturnId))
            foreach (var error in await ReturnedUnitErrors(document, products))
                Add(error);

        //a serial number is one unit: it is named once a document
        foreach (var productLines in document.Lines.Where(x => products.GetValueOrDefault(x.ProductId ?? "") is
                         { InventoryTrackingModeId: InventoryTrackingMode.SerialNumber })
                     .GroupBy(x => x.ProductId))
            if (UnitNumbers.Duplicates(productLines.SelectMany(x => x.SerialNumbers ?? [])).Count > 0)
                Add(UnitErrors.SerialDuplicate);

        return errors;
    }

    /// <summary>
    ///     What the serial numbers of a return receipt break: each was shipped for the order item it comes back for,
    ///     and only units shipped without a number may come back without one
    /// </summary>
    private async Task<IList<string>> ReturnedUnitErrors(InventoryDocument document,
        Dictionary<string, Product> products)
    {
        var errors = new List<string>();
        if (!document.Lines.Any(x => products.GetValueOrDefault(x.ProductId ?? "") is { } product &&
                                     InventoryTracking.Tracked(product) &&
                                     product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber))
            return errors;

        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(document.MerchandiseReturnId);
        if (merchandiseReturn == null)
            return errors;

        var shipped = await Shipped(merchandiseReturn.OrderId);
        foreach (var line in document.Lines)
        {
            if (products.GetValueOrDefault(line.ProductId ?? "") is not { } product ||
                !InventoryTracking.Tracked(product) ||
                product.InventoryTrackingModeId != InventoryTrackingMode.SerialNumber)
                continue;

            var orderItemId = merchandiseReturn.MerchandiseReturnItems
                .FirstOrDefault(x => x.Id == line.MerchandiseReturnItemId)?.OrderItemId;
            var (quantity, serialNumbers, _) = orderItemId != null && shipped.TryGetValue(orderItemId, out var value)
                ? value
                : (0, new List<string>(), new List<ShipmentItemBatch>());
            var numbers = line.SerialNumbers ?? [];
            var keys = serialNumbers.Select(UnitNumbers.Key).ToHashSet();
            if (numbers.Count > line.Quantity)
                errors.Add(UnitErrors.SerialCountMismatch);
            if (numbers.Any(x => !keys.Contains(UnitNumbers.Key(x))))
                errors.Add(UnitErrors.SerialNotShipped);
            if (line.Quantity - numbers.Count > quantity - serialNumbers.Count)
                errors.Add(UnitErrors.SerialRequired);
        }

        return errors;
    }

    /// <summary>
    ///     What a line of a product tracking its units lacks: the batch of what comes in, or a serial number for every
    ///     unit received, adjusted, moved or assigned. Whether the units are there to move is the ledger's check
    /// </summary>
    private static IEnumerable<string> UnitErrorsOf(InventoryDocument document, InventoryDocumentLine line,
        Product product)
    {
        if (!InventoryTracking.Tracked(product))
            yield break;

        var type = document.DocumentTypeId;
        if (product.InventoryTrackingModeId == InventoryTrackingMode.Batch)
        {
            if (string.IsNullOrWhiteSpace(line.BatchNumber) &&
                (type is InventoryDocumentType.Receipt or InventoryDocumentType.Assignment ||
                 (type == InventoryDocumentType.Adjustment && line.Quantity > 0)))
                yield return UnitErrors.BatchRequired;
            yield break;
        }

        if (type is InventoryDocumentType.Receipt or InventoryDocumentType.Adjustment or InventoryDocumentType.Transfer
                or InventoryDocumentType.Assignment &&
            (line.SerialNumbers?.Count ?? 0) != Math.Abs(line.Quantity))
            yield return UnitErrors.SerialCountMismatch;
    }

    /// <summary>
    ///     Whether the return receipts of the merchandise return, this one with its lines as they are now, take in more
    ///     of an item than was returned
    /// </summary>
    private async Task<bool> ReturnQuantityExceeded(InventoryDocument document)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(document.MerchandiseReturnId);
        if (merchandiseReturn == null)
            return false;

        var returned = (await Returned(merchandiseReturn))
            .GroupBy(x => (x.ReturnItemId, x.ProductId))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
        var received = await Received(merchandiseReturn.Id, document.Id);
        foreach (var line in document.Lines.Where(x => !string.IsNullOrEmpty(x.MerchandiseReturnItemId)))
            received[(line.MerchandiseReturnItemId, line.ProductId)] =
                received.GetValueOrDefault((line.MerchandiseReturnItemId, line.ProductId)) + line.Quantity;

        return received.Any(x => x.Value > returned.GetValueOrDefault(x.Key));
    }

    private async Task<string> StoreOf(string warehouseId)
    {
        return string.IsNullOrEmpty(warehouseId)
            ? ""
            : (await warehouseService.GetWarehouseById(warehouseId))?.StoreId ?? "";
    }

    private async Task<string> Prefix(InventoryDocumentType documentType)
    {
        var settings = await settingService.LoadSetting<InventorySettings>();
        return documentType switch {
            InventoryDocumentType.Receipt => settings.ReceiptNumberPrefix,
            InventoryDocumentType.Transfer => settings.TransferNumberPrefix,
            InventoryDocumentType.Adjustment => settings.AdjustmentNumberPrefix,
            InventoryDocumentType.StockCount => settings.StockCountNumberPrefix,
            InventoryDocumentType.ReturnReceipt => settings.ReturnReceiptNumberPrefix,
            InventoryDocumentType.Assignment => settings.AssignmentNumberPrefix,
            _ => throw new ArgumentOutOfRangeException(nameof(documentType))
        };
    }

    /// <summary>
    ///     The lines of the document that have a journal entry
    /// </summary>
    private async Task<HashSet<string>> BookedLines(string documentId)
    {
        var positions = await inventoryJournalRepository.ToListAsync(inventoryJournalRepository.Table
            .Where(x => x.ObjectType == nameof(InventoryDocument) && x.ObjectId == documentId)
            .Select(x => x.PositionId));
        return positions.ToHashSet();
    }

    private static string DocumentLockKey(string documentId)
    {
        return $"InventoryDocument:{documentId}";
    }

    private static string AttributesKey(IList<CustomAttribute> attributes)
    {
        return string.Join("|", (attributes ?? []).Select(x => $"{x.Key}={x.Value}").Order(StringComparer.Ordinal));
    }

    private static bool SameBooking(InventoryDocumentLine stored, InventoryDocumentLine line)
    {
        return line != null && line.ProductId == stored.ProductId && line.Quantity == stored.Quantity &&
               line.CountedQuantity == stored.CountedQuantity && line.UnitCost == stored.UnitCost &&
               line.Condition == stored.Condition && line.BatchNumber == stored.BatchNumber &&
               line.ExpiryDateUtc == stored.ExpiryDateUtc &&
               (line.SerialNumbers ?? []).SequenceEqual(stored.SerialNumbers ?? []) &&
               line.Attributes.Select(x => (x.Key, x.Value))
                   .SequenceEqual(stored.Attributes.Select(x => (x.Key, x.Value)));
    }
}
