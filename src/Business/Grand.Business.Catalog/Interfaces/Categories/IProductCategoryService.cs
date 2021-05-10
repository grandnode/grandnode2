using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Categories
{
    public interface IProductCategoryService
    {
        /// <summary>
        /// Gets product category mapping collection
        /// </summary>
        /// <param name="categoryId">Category id</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Product a category mapping collection</returns>
        Task<IPagedList<ProductsCategory>> GetProductCategoriesByCategoryId(string categoryId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

        /// <summary>
        /// Inserts a mapping for product category
        /// </summary>
        /// <param name="productCategory">>Product category mapping</param>
        /// <param name="productId">Product id</param>
        Task InsertProductCategory(ProductCategory productCategory, string productId);

        /// <summary>
        /// Updates a mapping for product category 
        /// </summary>
        /// <param name="productCategory">>Product category mapping</param>
        /// <param name="productId">Product id</param>
        Task UpdateProductCategory(ProductCategory productCategory, string productId);

        /// <summary>
        /// Deletes a mapping for product category
        /// </summary>
        /// <param name="productCategory">Product category</param>
        /// <param name="productId">Product id</param>
        Task DeleteProductCategory(ProductCategory productCategory, string productId);

    }
}
