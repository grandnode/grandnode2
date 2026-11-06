using Grand.Domain.Catalog;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Extensions;

public static class OrderItemWarehouse
{
    /// <summary>
    ///     The warehouse an order item is served from: the one chosen in the cart, else the store's default, else the
    ///     product's own when it is kept in a single warehouse
    /// </summary>
    public static string Resolve(ShoppingCartItem item, Product product, string storeDefaultWarehouseId)
    {
        var warehouseId = !string.IsNullOrEmpty(item.WarehouseId)
            ? item.WarehouseId
            : storeDefaultWarehouseId;
        if (!product.UseMultipleWarehouses && string.IsNullOrEmpty(warehouseId))
            if (!string.IsNullOrEmpty(product.WarehouseId))
                warehouseId = product.WarehouseId;

        return warehouseId;
    }
}
