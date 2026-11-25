using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Module.Api.DTOs.Inventory;
using Grand.Module.Api.Queries.Models.Inventory;
using Grand.Mediator;

namespace Grand.Module.Api.Queries.Handlers.Inventory;

/// <summary>
///     Reads a page of the inventory journal for an external system to pull: ordered by entry id, older than a settle
///     window so a cursor never skips an entry that became visible late, with the SKU and warehouse code of each entry
///     resolved in one read per page
/// </summary>
public class GetStockJournalQueryHandler(
    IRepository<InventoryJournal> journalRepository,
    IRepository<Product> productRepository,
    IRepository<Warehouse> warehouseRepository) : IRequestHandler<GetStockJournalQuery, StockJournalResultDto>
{
    /// <summary>
    ///     Entries newer than this are not returned: with several instances an entry created earlier can become visible
    ///     after a later one, and a cursor would skip it
    /// </summary>
    private const int SettleSeconds = 30;

    private const int MaxTake = 1000;

    public async Task<StockJournalResultDto> Handle(GetStockJournalQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, MaxTake);
        var settleCutoff = DateTime.UtcNow.AddSeconds(-SettleSeconds);

        var warehouseIds = await WarehouseIds(request);
        //a code no warehouse has matches no entry; dropping the filter would hand over every warehouse's movements
        if (warehouseIds.Count == 0 && !string.IsNullOrWhiteSpace(request.WarehouseCode))
            return new StockJournalResultDto { Next = request.After };

        var movementTypes = request.MovementTypes ?? [];

        var query = journalRepository.Table.Where(x => x.CreateDateUtc <= settleCutoff);
        if (!string.IsNullOrEmpty(request.After))
            query = query.Where(x => string.Compare(x.Id, request.After) > 0);
        if (warehouseIds.Count > 0)
            query = query.Where(x => x.WarehouseId != null && warehouseIds.Contains(x.WarehouseId));
        if (movementTypes.Count > 0)
            query = query.Where(x => movementTypes.Contains(x.MovementTypeId));
        if (request.ExcludeOwn && !string.IsNullOrEmpty(request.CustomerId))
            query = query.Where(x => x.CustomerId != request.CustomerId);

        //one more than asked tells whether another page follows. The cursor is the last id returned, or the one asked
        //with when nothing is new: an empty cursor reads from the beginning, so the last page must not hand one back
        var entries = await journalRepository.ToListAsync(query.OrderBy(x => x.Id).Take(take + 1),
            cancellationToken);
        var page = entries.Take(take).ToList();

        return new StockJournalResultDto {
            Next = page.Count > 0 ? page[^1].Id : request.After,
            HasMore = entries.Count > take,
            Entries = await BuildEntries(page)
        };
    }

    private async Task<List<string>> WarehouseIds(GetStockJournalQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.WarehouseId))
            return [request.WarehouseId.Trim()];

        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            return (await warehouseRepository.ToListAsync(warehouseRepository.Table
                    .Where(x => x.Code == request.WarehouseCode.Trim())))
                .Select(x => x.Id).ToList();

        return [];
    }

    private async Task<IList<StockJournalEntryDto>> BuildEntries(IList<InventoryJournal> entries)
    {
        var productIds = entries.Select(x => x.ProductId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<string, Product>()
            : (await productRepository.ToListAsync(productRepository.Table.Where(x => productIds.Contains(x.Id))))
            .ToDictionary(x => x.Id);

        var warehouseIds = entries.Select(x => x.WarehouseId).Where(x => !string.IsNullOrEmpty(x)).Distinct()
            .ToList();
        var warehouses = warehouseIds.Count == 0
            ? new Dictionary<string, Warehouse>()
            : (await warehouseRepository.ToListAsync(warehouseRepository.Table.Where(x => warehouseIds.Contains(x.Id))))
            .ToDictionary(x => x.Id);

        return entries.Select(x => ToDto(x, products.GetValueOrDefault(x.ProductId),
            x.WarehouseId != null ? warehouses.GetValueOrDefault(x.WarehouseId) : null)).ToList();
    }

    private static StockJournalEntryDto ToDto(InventoryJournal entry, Product? product, Warehouse? warehouse)
    {
        var sku = product?.Sku ?? "";
        if (product != null && !string.IsNullOrEmpty(entry.CombinationId))
            sku = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == entry.CombinationId)?.Sku ?? sku;

        return new StockJournalEntryDto {
            Id = entry.Id,
            CreatedOnUtc = entry.CreateDateUtc,
            ProductId = entry.ProductId,
            Sku = sku,
            CombinationId = entry.CombinationId,
            WarehouseId = entry.WarehouseId,
            WarehouseCode = warehouse?.Code ?? "",
            InQty = entry.InQty,
            OutQty = entry.OutQty,
            MovementType = entry.MovementTypeId,
            ObjectType = entry.ObjectType,
            ObjectId = entry.ObjectId,
            Reference = entry.Reference,
            Comments = entry.Comments,
            CustomerId = entry.CustomerId,
            StoreId = entry.StoreId,
            BatchNumber = entry.BatchNumber,
            SerialNumbers = entry.SerialNumbers ?? [],
            ExpiryDateUtc = entry.ExpiryDateUtc,
            IdempotencyKey = entry.IdempotencyKey
        };
    }
}
