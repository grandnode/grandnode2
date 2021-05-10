using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Collections
{
    /// <summary>
    /// Collection service
    /// </summary>
    public partial interface ICollectionService
    {

        /// <summary>
        /// Gets all collections
        /// </summary>
        /// <param name="collectionName">Collection name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Collections</returns>
        Task<IPagedList<Collection>> GetAllCollections(string collectionName = "",
            string storeId = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false);

        /// <summary>
        /// Gets all collections featired products displayed on the home page
        /// </summary>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Collections</returns>
        Task<IList<Collection>> GetAllCollectionFeaturedProductsOnHomePage(bool showHidden = false);

        /// <summary>
        /// Gets an existing collection by id
        /// </summary>
        /// <param name="collectionId">Collection identifier</param>
        /// <returns>Collection</returns>
        Task<Collection> GetCollectionById(string collectionId);

        /// <summary>
        /// Inserts a new collection
        /// </summary>
        /// <param name="collection">Collection</param>
        Task InsertCollection(Collection collection);

        /// <summary>
        /// Updates the existing collection
        /// </summary>
        /// <param name="collection">Collection</param>
        Task UpdateCollection(Collection collection);

        /// <summary>
        /// Deletes an existing collection
        /// </summary>
        /// <param name="collection">Collection</param>
        Task DeleteCollection(Collection collection);

        /// <summary>
        /// Gets all manfucaturers by discount id
        /// </summary>
        /// <param name="discountId">Discount id </param>
        /// <returns>Product collection mapping</returns>
        Task<IList<Collection>> GetAllCollectionsByDiscount(string discountId);

    }
}
