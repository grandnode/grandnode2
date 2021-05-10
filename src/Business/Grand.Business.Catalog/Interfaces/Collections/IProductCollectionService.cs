using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Collections
{
    public interface IProductCollectionService
    {
        /// <summary>
        /// Gets product collection by collection id
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="storeId">Store id</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Product collection collection</returns>
        Task<IPagedList<ProductsCollection>> GetProductCollectionsByCollectionId(string collectionId, string storeId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);


        /// <summary>
        /// Inserts a new product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product ident</param>
        Task InsertProductCollection(ProductCollection productCollection, string productId);

        /// <summary>
        /// Updates the existing product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product id</param>
        Task UpdateProductCollection(ProductCollection productCollection, string productId);

        /// <summary>
        /// Deletes the existing product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product id</param>
        Task DeleteProductCollection(ProductCollection productCollection, string productId);

    }
}
