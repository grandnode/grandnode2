using Grand.Data;
using Grand.Data.Inventory;
using Grand.Domain.Admin;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationInventoryLedger : IMigration
{
    private const int PageSize = 500;

    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("c4b7e2a9-5d31-4f86-a0e3-7b9d2c6f4e18");
    public string Name => "Inventory journal: indexes, legacy marking, opening balances, permission, admin menu";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var journalRepository = serviceProvider.GetRequiredService<IRepository<InventoryJournal>>();
        var documentRepository = serviceProvider.GetRequiredService<IRepository<InventoryDocument>>();
        var productRepository = serviceProvider.GetRequiredService<IRepository<Product>>();
        var warehouseRepository = serviceProvider.GetRequiredService<IRepository<Warehouse>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationInventoryLedger>>();
        var success = true;

        try
        {
            await InventoryIndexes.Create(databaseContext, journalRepository, documentRepository);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - InventoryLedger indexes (2.5)");
            success = false;
        }

        try
        {
            //the old rows first, so their type is never read as an opening balance
            await MarkLegacy(journalRepository);
            await OpenBalances(productRepository, journalRepository, warehouseRepository);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - InventoryLedger opening balances (2.5)");
            success = false;
        }

        try
        {
            await AddPermission(serviceProvider);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - InventoryLedger permission (2.5)");
            success = false;
        }

        try
        {
            await AddSiteMap(serviceProvider);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - InventoryLedger admin menu (2.5)");
            success = false;
        }

        return success;
    }

    /// <summary>
    ///     The shipment entries written before every change was journaled: they stay visible and count for reversing
    ///     their shipment, never for the stock. Such a row has no movement type yet, which a not-equal filter matches
    ///     on MongoDB and LiteDB alike
    /// </summary>
    public static Task MarkLegacy(IRepository<InventoryJournal> journalRepository)
    {
        return journalRepository.UpdateManyAsync(x => x.ObjectType == nameof(Shipment) &&
                                                      x.MovementTypeId != InventoryMovementType.Shipment &&
                                                      x.MovementTypeId != InventoryMovementType.ShipmentReversal,
            UpdateBuilder<InventoryJournal>.Create().Set(x => x.MovementTypeId, InventoryMovementType.Legacy));
    }

    /// <summary>
    ///     One opening balance per stock bucket of every product that tracks stock, with the stock stored now; the
    ///     product and combination sums are worked out from the warehouse rows first. A product that has an opening
    ///     balance already is left alone, so running again writes nothing
    /// </summary>
    public static async Task OpenBalances(IRepository<Product> productRepository,
        IRepository<InventoryJournal> journalRepository, IRepository<Warehouse> warehouseRepository)
    {
        var stores = (await warehouseRepository.ToListAsync(warehouseRepository.Table))
            .ToDictionary(x => x.Id, x => x.StoreId ?? "");
        var query = productRepository.Table.Where(x =>
                x.ManageInventoryMethodId == ManageInventoryMethod.ManageStock ||
                x.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            .OrderBy(x => x.Id);

        for (var pageIndex = 0;; pageIndex++)
        {
            var products = await productRepository.PagedAsync(query, pageIndex, PageSize);
            foreach (var product in products)
            {
                if (await journalRepository.AnyAsync(journalRepository.Table.Where(x =>
                        x.ProductId == product.Id && x.MovementTypeId == InventoryMovementType.OpeningBalance)))
                    continue;

                Sum(product);
                var revision = product.StockRevision;
                foreach (var (combinationId, attributes, warehouseId, stock) in Buckets(product))
                    await journalRepository.InsertAsync(new InventoryJournal {
                        ProductId = product.Id,
                        CombinationId = combinationId,
                        Attributes = attributes,
                        WarehouseId = warehouseId,
                        StoreId = stores.GetValueOrDefault(warehouseId) ?? "",
                        InQty = Math.Max(0, stock),
                        OutQty = Math.Max(0, -stock),
                        MovementTypeId = InventoryMovementType.OpeningBalance,
                        CreateDateUtc = DateTime.UtcNow,
                        ObjectType = nameof(Product),
                        ObjectId = product.Id,
                        StockRevision = ++revision
                    });

                await productRepository.UpdateOneAsync(x => x.Id == product.Id,
                    UpdateBuilder<Product>.Create()
                        .Set(x => x.StockQuantity, product.StockQuantity)
                        .Set(x => x.ReservedQuantity, product.ReservedQuantity)
                        .Set(x => x.ProductAttributeCombinations, product.ProductAttributeCombinations)
                        .Set(x => x.StockRevision, revision));
            }

            if (!products.HasNextPage)
                return;
        }
    }

    /// <summary>
    ///     With multiple warehouses a product's or combination's stock is the sum of its rows, and a product stocked by
    ///     attributes holds the sum of its combinations - as the stock ledger keeps them
    /// </summary>
    private static void Sum(Product product)
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock && product.UseMultipleWarehouses)
        {
            product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
            product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);
        }

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
            return;

        if (product.UseMultipleWarehouses)
            foreach (var combination in product.ProductAttributeCombinations)
            {
                combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);
            }

        product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
        product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
    }

    private static IEnumerable<(string CombinationId, IList<CustomAttribute> Attributes, string WarehouseId, int Stock)>
        Buckets(Product product)
    {
        var warehouseId = product.WarehouseId ?? "";
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
        {
            if (!product.UseMultipleWarehouses)
                yield return ("", [], warehouseId, product.StockQuantity);
            else
                foreach (var row in product.ProductWarehouseInventory)
                    yield return ("", [], row.WarehouseId, row.StockQuantity);
            yield break;
        }

        foreach (var combination in product.ProductAttributeCombinations)
            if (!product.UseMultipleWarehouses)
                yield return (combination.Id, combination.Attributes, warehouseId, combination.StockQuantity);
            else
                foreach (var row in combination.WarehouseInventory)
                    yield return (combination.Id, combination.Attributes, row.WarehouseId, row.StockQuantity);
    }

    private static async Task AddPermission(IServiceProvider serviceProvider)
    {
        var permissionRepository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var groupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();

        if (await permissionRepository.AnyAsync(
                permissionRepository.Table.Where(x => x.SystemName == PermissionSystemName.InventoryDocuments)))
            return;

        var permission = new Permission {
            Name = StandardPermission.ManageInventoryDocuments.Name,
            SystemName = StandardPermission.ManageInventoryDocuments.SystemName,
            Area = StandardPermission.ManageInventoryDocuments.Area,
            Category = StandardPermission.ManageInventoryDocuments.Category,
            Actions = StandardPermission.ManageInventoryDocuments.Actions.ToList()
        };
        foreach (var systemName in new[] { SystemCustomerGroupNames.Administrators, SystemCustomerGroupNames.StoreManager })
        {
            var group = await groupRepository.FirstOrDefaultAsync(
                groupRepository.Table.Where(x => x.SystemName == systemName));
            if (group != null)
                permission.CustomerGroups.Add(group.Id);
        }

        await permissionRepository.InsertAsync(permission);
    }

    private static async Task AddSiteMap(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var catalog = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Catalog"));
        if (catalog == null || catalog.ChildNodes.Any(x => x.SystemName == "Inventory documents"))
            return;

        catalog.ChildNodes.Add(new AdminSiteMap {
            SystemName = "Inventory documents",
            ResourceName = "Admin.Inventory.Documents",
            PermissionNames = new List<string> { PermissionSystemName.InventoryDocuments },
            ControllerName = "InventoryDocument",
            ActionName = "List",
            DisplayOrder = 20,
            IconClass = "bi bi-record-circle"
        });
        catalog.ChildNodes.Add(new AdminSiteMap {
            SystemName = "Stock at date",
            ResourceName = "Admin.Inventory.StockAtDate",
            PermissionNames = new List<string> { PermissionSystemName.InventoryDocuments },
            ControllerName = "InventoryReport",
            ActionName = "StockAtDate",
            DisplayOrder = 21,
            IconClass = "bi bi-record-circle"
        });
        if (!catalog.PermissionNames.Contains(PermissionSystemName.InventoryDocuments))
            catalog.PermissionNames.Add(PermissionSystemName.InventoryDocuments);
        await repository.UpdateAsync(catalog);
    }
}
