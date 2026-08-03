using Grand.Domain;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

public interface IWarehouseService
{
    /// <summary>
    ///     Gets a warehouse
    /// </summary>
    /// <param name="warehouseId">The warehouse identifier</param>
    /// <returns>Warehouse</returns>
    Task<Warehouse> GetWarehouseById(string warehouseId);

    /// <summary>
    ///     Gets all warehouses
    /// </summary>
    /// <param name="storeId">Store identifier; empty to return all warehouses</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Warehouses</returns>
    Task<IPagedList<Warehouse>> GetAllWarehouses(string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Inserts a warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    Task InsertWarehouse(Warehouse warehouse);

    /// <summary>
    ///     Updates the warehouse
    /// </summary>
    /// <param name="warehouse">Warehouse</param>
    Task UpdateWarehouse(Warehouse warehouse);

    /// <summary>
    ///     Deletes a warehouse
    /// </summary>
    /// <param name="warehouse">The warehouse</param>
    Task DeleteWarehouse(Warehouse warehouse);
}