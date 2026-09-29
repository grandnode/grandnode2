#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Catalog.Services.Products;

public class StockQuantityService : IStockQuantityService
{
    public virtual int GetTotalStockQuantity(Product product, bool useReservedQuantity = true,
        string warehouseId = "", bool total = false)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock) return 0;

        if (product.UseMultipleWarehouses)
        {
            if (total)
                return useReservedQuantity
                    ? product.ProductWarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity)
                    : product.ProductWarehouseInventory.Sum(x => x.StockQuantity);

            var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null) return 0;
            var result = pwi.StockQuantity;
            if (useReservedQuantity) result -= pwi.ReservedQuantity;

            return result;
        }

        if (string.IsNullOrEmpty(warehouseId) || string.IsNullOrEmpty(product.WarehouseId))
            return product.StockQuantity - (useReservedQuantity ? product.ReservedQuantity : 0);

        if (product.WarehouseId == warehouseId)
            return product.StockQuantity - (useReservedQuantity ? product.ReservedQuantity : 0);

        return 0;
    }

    public virtual int GetTotalStockQuantityForCombination(Product product, ProductAttributeCombination combination,
        bool useReservedQuantity = true, string warehouseId = "")
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(combination);

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes) return 0;

        if (product.UseMultipleWarehouses)
        {
            var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null) return 0;
            var result = pwi.StockQuantity;
            if (useReservedQuantity) result -= pwi.ReservedQuantity;

            return result;
        }

        if (string.IsNullOrEmpty(warehouseId) || string.IsNullOrEmpty(product.WarehouseId))
            return combination.StockQuantity - (useReservedQuantity ? combination.ReservedQuantity : 0);

        if (product.WarehouseId == warehouseId)
            return combination.StockQuantity - (useReservedQuantity ? combination.ReservedQuantity : 0);

        return 0;
    }

    public virtual (string resource, object? arg0) FormatStockMessage(Product product, string warehouseId,
        IList<CustomAttribute> attributes)
    {
        var status = GetStockStatus(product, warehouseId, attributes);
        return (status.Resource, status.Arg0);
    }

    public virtual StockStatus GetStockStatus(Product product, string warehouseId,
        IList<CustomAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(product);

        var status = product.ManageInventoryMethodId switch {
            ManageInventoryMethod.ManageStock => StockInventoryStatus(product, warehouseId),
            ManageInventoryMethod.ManageStockByAttributes => StockByAttributesStatus(product, warehouseId,
                attributes),
            //stock is not tracked, so nothing stops the product from being bought
            _ => new StockStatus(ProductAvailability.InStock, string.Empty, null)
        };

        //StockAvailability only decides whether the customer is shown the message -
        //a product that keeps its stock to itself is still in stock or out of it
        if (!product.StockAvailability) status = status with { Resource = string.Empty, Arg0 = null };

        //a product not released yet is sold on pre-order whatever the warehouse holds
        if (product.AvailableForPreOrder &&
            (!product.PreOrderDateTimeUtc.HasValue || product.PreOrderDateTimeUtc.Value >= DateTime.UtcNow))
            status = status with { Availability = ProductAvailability.PreOrder };

        return status;
    }

    private StockStatus StockByAttributesStatus(Product product, string warehouseId,
        IList<CustomAttribute> attributes)
    {
        var combination = product.FindProductAttributeCombination(attributes);
        if (combination == null)
            return new StockStatus(null, "Products.Availability.AttributeCombinationsNotExists", null);

        var stockQuantity = GetTotalStockQuantityForCombination(product, combination, warehouseId: warehouseId);
        if (stockQuantity > 0)
            return product.DisplayStockQuantity
                ? new StockStatus(ProductAvailability.InStock, "Products.Availability.InStockWithQuantity",
                    stockQuantity)
                : new StockStatus(ProductAvailability.InStock, "Products.Availability.InStock", null);

        return product.BackorderModeId switch {
            BackorderMode.NoBackorders => new StockStatus(ProductAvailability.OutOfStock,
                "Products.Availability.Attributes.OutOfStock", null),
            BackorderMode.AllowQtyBelowZero => new StockStatus(ProductAvailability.BackOrder,
                "Products.Availability.Attributes.Backordering", null),
            _ => combination.AllowOutOfStockOrders
                ? new StockStatus(ProductAvailability.BackOrder, string.Empty, null)
                : new StockStatus(ProductAvailability.OutOfStock, "Products.Availability.Attributes.OutOfStock",
                    null)
        };
    }

    private StockStatus StockInventoryStatus(Product product, string warehouseId)
    {
        var stockQuantity = GetTotalStockQuantity(product, warehouseId: warehouseId);
        if (stockQuantity > 0)
            return product.DisplayStockQuantity
                ? new StockStatus(ProductAvailability.InStock, "Products.Availability.InStockWithQuantity",
                    stockQuantity)
                : new StockStatus(ProductAvailability.InStock, "Products.Availability.InStock", null);

        return product.BackorderModeId switch {
            BackorderMode.NoBackorders => new StockStatus(ProductAvailability.OutOfStock,
                "Products.Availability.OutOfStock", null),
            BackorderMode.AllowQtyBelowZero => new StockStatus(ProductAvailability.BackOrder,
                "Products.Availability.Backordering", null),
            _ => new StockStatus(ProductAvailability.OutOfStock, string.Empty, null)
        };
    }
}