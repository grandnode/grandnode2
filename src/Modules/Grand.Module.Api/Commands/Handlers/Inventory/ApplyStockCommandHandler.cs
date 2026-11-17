using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Module.Api.Commands.Models.Inventory;
using Grand.Module.Api.DTOs.Inventory;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Inventory;

/// <summary>
///     Applies each line of a bulk stock write on its own: a rejected line does not stop the others, and a retried line
///     with an already booked idempotency key is reported as "already applied", not a failure
/// </summary>
public class ApplyStockCommandHandler(
    IStockMovementService stockMovementService,
    IRepository<Product> productRepository,
    IRepository<Warehouse> warehouseRepository) : IRequestHandler<ApplyStockCommand, IList<StockApplyResultDto>>
{
    private const string AppliedStatus = "Applied";
    private const string AlreadyAppliedStatus = "AlreadyApplied";
    private const string RejectedStatus = "Rejected";

    public async Task<IList<StockApplyResultDto>> Handle(ApplyStockCommand request,
        CancellationToken cancellationToken)
    {
        var targets = await ReadTargets(request.Lines);
        var results = new List<StockApplyResultDto>();
        foreach (var line in request.Lines)
            results.Add(await ApplyLine(line, request.CustomerId, targets));

        return results;
    }

    /// <summary>
    ///     The products and warehouses the lines name, read in one query per kind for the whole request: by id, by
    ///     product SKU, by combination SKU and by warehouse code. They only resolve a line; the ledger reads the product
    ///     again under its lock
    /// </summary>
    private async Task<Targets> ReadTargets(IList<StockApplyLineDto> lines)
    {
        static List<string> Trimmed(IEnumerable<string> values)
        {
            return values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        }

        var productIds = Trimmed(lines.Select(x => x.ProductId));
        var skus = Trimmed(lines.Select(x => x.Sku));
        var codes = Trimmed(lines.Select(x => x.WarehouseCode));

        var byId = productIds.Count == 0
            ? []
            : await productRepository.ToListAsync(productRepository.Table.Where(x => productIds.Contains(x.Id)));
        var bySku = skus.Count == 0
            ? []
            : await productRepository.ToListAsync(productRepository.Table.Where(x => skus.Contains(x.Sku)));
        var byCombinationSku = skus.Count == 0
            ? []
            : await productRepository.ToListAsync(productRepository.Table
                .Where(x => x.ProductAttributeCombinations.Any(c => skus.Contains(c.Sku))));
        var warehouses = codes.Count == 0
            ? []
            : await warehouseRepository.ToListAsync(warehouseRepository.Table.Where(x => codes.Contains(x.Code)));

        return new Targets(byId.ToDictionary(x => x.Id), bySku, byCombinationSku, warehouses);
    }

    private async Task<StockApplyResultDto> ApplyLine(StockApplyLineDto line, string customerId, Targets targets)
    {
        if (string.IsNullOrWhiteSpace(line.Key))
            return Rejected(line, StockErrors.KeyRequired);
        if (line.Key.Length > 100)
            return Rejected(line, StockErrors.KeyTooLong);

        var setTo = string.Equals(line.Mode?.Trim(), "Set", StringComparison.OrdinalIgnoreCase);
        var changeTo = string.Equals(line.Mode?.Trim(), "Change", StringComparison.OrdinalIgnoreCase);
        if (!setTo && !changeTo)
            return Rejected(line, StockErrors.ModeInvalid);

        var hasProductId = !string.IsNullOrWhiteSpace(line.ProductId);
        var hasSku = !string.IsNullOrWhiteSpace(line.Sku);
        if (hasProductId == hasSku)
            return Rejected(line, hasProductId ? StockErrors.ProductTargetBoth : StockErrors.ProductTargetRequired);

        if (!string.IsNullOrWhiteSpace(line.WarehouseId) && !string.IsNullOrWhiteSpace(line.WarehouseCode))
            return Rejected(line, StockErrors.WarehouseTargetBoth);

        var (product, combination, warehouseId, error) = Resolve(line, hasProductId, targets);
        if (error != null)
            return Rejected(line, error);

        //the stock of a batch- or serial-tracked product names its units; a target is the whole bucket, not one unit
        if (InventoryTracking.Tracked(product) && setTo)
            return Rejected(line, StockErrors.TrackedSetNotSupported);

        //a serial line names exactly |quantity| numbers, and a positive batch line the batch it adds to
        if (InventoryTracking.Tracked(product) && changeTo)
        {
            if (product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber &&
                (line.SerialNumbers?.Count ?? 0) != Math.Abs(line.Quantity))
                return Rejected(line, UnitErrors.SerialCountMismatch);
            if (product.InventoryTrackingModeId == InventoryTrackingMode.Batch && line.Quantity > 0 &&
                string.IsNullOrWhiteSpace(line.BatchNumber))
                return Rejected(line, UnitErrors.BatchRequired);
        }

        var change = new StockChange(
            combination?.Attributes ?? line.Attributes ?? [],
            warehouseId ?? "",
            line.Quantity,
            setTo,
            InventoryMovementType.Api,
            new StockSource("Api", product.Id,
                Reference: string.IsNullOrWhiteSpace(line.Reference) ? product.Sku ?? "" : line.Reference.Trim(),
                CustomerId: customerId,
                Comment: string.IsNullOrWhiteSpace(line.Comment) ? null : line.Comment.Trim()))
        {
            BatchNumber = string.IsNullOrWhiteSpace(line.BatchNumber) ? null : line.BatchNumber.Trim(),
            ExpiryDateUtc = line.ExpiryDate,
            SerialNumbers = (line.SerialNumbers ?? []).Select(x => x.Trim()).ToList(),
            IdempotencyKey = line.Key
        };

        var errors = await stockMovementService.Apply(product.Id, [change]);
        if (errors.Count > 0 && !errors.Contains(StockErrors.AlreadyApplied))
            return Rejected(line, errors[0]);

        var fresh = await productRepository.GetByIdAsync(product.Id);
        return new StockApplyResultDto {
            Key = line.Key,
            Status = errors.Count == 0 ? AppliedStatus : AlreadyAppliedStatus,
            Stock = StockAfter(fresh, combination, warehouseId)
        };
    }

    private static (Product Product, ProductAttributeCombination Combination, string WarehouseId, string Error)
        Resolve(StockApplyLineDto line, bool hasProductId, Targets targets)
    {
        Product product;
        ProductAttributeCombination combination = null;
        if (hasProductId)
        {
            product = targets.ProductsById.GetValueOrDefault(line.ProductId.Trim());
            if (product == null)
                return (null, null, null, StockErrors.ProductNotFound);

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                combination = line.Attributes is { Count: > 0 }
                    ? product.FindProductAttributeCombination(line.Attributes)
                    : null;
                if (combination == null)
                    return (null, null, null, StockErrors.CombinationNotFound);
            }
        }
        else
        {
            var sku = line.Sku.Trim();
            var matches = targets.BySku.Where(x => x.Sku == sku)
                .Select(x => (Product: x, Combination: (ProductAttributeCombination)null))
                .ToList();
            foreach (var owner in targets.ByCombinationSku)
            foreach (var ownerCombination in owner.ProductAttributeCombinations.Where(c => c.Sku == sku))
                matches.Add((owner, ownerCombination));

            if (matches.Count == 0)
                return (null, null, null, StockErrors.SkuNotFound);
            if (matches.Count > 1)
                return (null, null, null, StockErrors.SkuAmbiguous);

            (product, combination) = matches[0];
        }

        string warehouseId = null;
        if (!string.IsNullOrWhiteSpace(line.WarehouseId))
            warehouseId = line.WarehouseId.Trim();
        else if (!string.IsNullOrWhiteSpace(line.WarehouseCode))
        {
            var code = line.WarehouseCode.Trim();
            var matches = targets.Warehouses.Where(x => x.Code == code).ToList();
            if (matches.Count != 1)
                return (null, null, null,
                    matches.Count == 0 ? StockErrors.WarehouseCodeNotFound : StockErrors.WarehouseCodeAmbiguous);
            warehouseId = matches[0].Id;
        }

        //the warehouse must already be the product's: a single-warehouse product books nowhere else, and a
        //multi-warehouse product accepts only a warehouse it already has a row for
        if (product.UseMultipleWarehouses)
        {
            if (string.IsNullOrEmpty(warehouseId))
                return (null, null, null, StockErrors.WarehouseRequired);

            var hasRow = product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock
                ? product.ProductWarehouseInventory.Any(x => x.WarehouseId == warehouseId)
                : combination != null && combination.WarehouseInventory.Any(x => x.WarehouseId == warehouseId);
            if (!hasRow)
                return (null, null, null, StockErrors.WarehouseNotOnProduct);
        }
        else if (!string.IsNullOrEmpty(warehouseId) && warehouseId != (product.WarehouseId ?? ""))
        {
            return (null, null, null, StockErrors.WarehouseNotOnProduct);
        }

        return (product, combination, warehouseId, null);
    }

    private static int? StockAfter(Product product, ProductAttributeCombination combination, string warehouseId)
    {
        if (product == null)
            return null;

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
        {
            if (!product.UseMultipleWarehouses)
                return product.StockQuantity;
            return product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId)?.StockQuantity
                   ?? 0;
        }

        var freshCombination = combination == null
            ? null
            : product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == combination.Id);
        if (freshCombination == null)
            return product.StockQuantity;

        if (!product.UseMultipleWarehouses)
            return freshCombination.StockQuantity;
        return freshCombination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId)?.StockQuantity ?? 0;
    }

    private static StockApplyResultDto Rejected(StockApplyLineDto line, string error)
    {
        return new StockApplyResultDto { Key = line.Key, Status = RejectedStatus, Error = error };
    }

    private sealed record Targets(
        Dictionary<string, Product> ProductsById,
        IList<Product> BySku,
        IList<Product> ByCombinationSku,
        IList<Warehouse> Warehouses);
}
