#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Products;
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
        ArgumentNullException.ThrowIfNull(product);

        var stockMessage = string.Empty;

        return product.ManageInventoryMethodId switch {
            ManageInventoryMethod.ManageStock => StockInventoryFormatStockMessage(product, warehouseId,
                stockMessage),
            ManageInventoryMethod.ManageStockByAttributes => StockByAttributesFormatStockMessage(product,
                warehouseId, attributes, stockMessage),
            _ => (stockMessage, null)
        };
    }

    private (string resource, object? arg0) StockByAttributesFormatStockMessage(Product product, string warehouseId,
        IList<CustomAttribute> attributes, string stockMessage)
    {
        if (!product.StockAvailability) return (stockMessage, null);

        var combination = product.FindProductAttributeCombination(attributes);
        if (combination != null)
        {
            //combination exists
            var stockQuantity =
                GetTotalStockQuantityForCombination(product, combination, warehouseId: warehouseId);
            if (stockQuantity > 0)
            {
                if (product.DisplayStockQuantity)
                    //display "in stock" with stock quantity
                    return ("Products.Availability.InStockWithQuantity", stockQuantity);

                //display "in stock" without stock quantity
                {
                    return ("Products.Availability.InStock", null);
                }
            }

            //out of stock
            switch (product.BackorderModeId)
            {
                case BackorderMode.NoBackorders:
                {
                    return ("Products.Availability.Attributes.OutOfStock", null);
                }
                case BackorderMode.AllowQtyBelowZero:
                {
                    return ("Products.Availability.Attributes.Backordering", null);
                }
            }

            if (!combination.AllowOutOfStockOrders) return ("Products.Availability.Attributes.OutOfStock", null);
        }
        else
        {
            return ("Products.Availability.AttributeCombinationsNotExists", null);
        }

        return (stockMessage, null);
    }

    private (string resource, object? arg0) StockInventoryFormatStockMessage(Product product, string warehouseId,
        string stockMessage)
    {
        if (!product.StockAvailability) return (stockMessage, null);

        var stockQuantity = GetTotalStockQuantity(product, warehouseId: warehouseId);
        if (stockQuantity > 0)
        {
            if (product.DisplayStockQuantity) return ("Products.Availability.InStockWithQuantity", stockQuantity);

            {
                return ("Products.Availability.InStock", null);
            }
        }

        switch (product.BackorderModeId)
        {
            case BackorderMode.NoBackorders:
            {
                return ("Products.Availability.OutOfStock", null);
            }
            case BackorderMode.AllowQtyBelowZero:
            {
                return ("Products.Availability.Backordering", null);
            }
        }

        return (stockMessage, null);
    }
}