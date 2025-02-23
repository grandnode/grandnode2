#nullable enable

using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Common.Stores;

/// <summary>
///     Store service interface
/// </summary>
public interface IStoreService
{
    /// <summary>
    ///     Gets all stores
    /// </summary>
    /// <returns>Stores</returns>
    Task<IList<Store>> GetAllStores();

    /// <summary>
    ///     Gets a store
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Store</returns>
    Task<Store> GetStoreById(string storeId);

    /// <summary>
    ///     Inserts a store
    /// </summary>
    /// <param name="store">Store</param>
    Task InsertStore(Store store);

    /// <summary>
    ///     Updates the store
    /// </summary>
    /// <param name="store">Store</param>
    Task UpdateStore(Store store);

    /// <summary>
    ///     Deletes a store
    /// </summary>
    /// <param name="store">Store</param>
    Task DeleteStore(Store store);

    /// <summary>
    /// Get store by host
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    Task<Store?> GetStoreByHost(string host);
}