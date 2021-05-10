using Grand.Domain.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Stores
{
    /// <summary>
    /// Store service interface
    /// </summary>
    public partial interface IStoreService
    {
       
        /// <summary>
        /// Gets all stores
        /// </summary>
        /// <returns>Stores</returns>
        Task<IList<Store>> GetAllStores();

        /// <summary>
        /// Gets all stores
        /// </summary>
        /// <returns>Stores</returns>
        IList<Store> GetAll();

        /// <summary>
        /// Gets a store 
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Store</returns>
        Task<Store> GetStoreById(string storeId);

        /// <summary>
        /// Inserts a store
        /// </summary>
        /// <param name="store">Store</param>
        Task InsertStore(Store store);

        /// <summary>
        /// Updates the store
        /// </summary>
        /// <param name="store">Store</param>
        Task UpdateStore(Store store);

        /// <summary>
        /// Deletes a store
        /// </summary>
        /// <param name="store">Store</param>
        Task DeleteStore(Store store);

    }
}