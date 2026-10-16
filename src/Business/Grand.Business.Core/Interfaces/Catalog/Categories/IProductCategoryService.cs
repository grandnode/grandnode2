using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Categories;

public interface IProductCategoryService
{
    /// <summary>
    ///     Gets product category mapping collection
    /// </summary>
    /// <param name="categoryId">Category id</param>
    /// <param name="access">Whose view of the products to return, and the store whose limitations apply</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Product a category mapping collection</returns>
    Task<IPagedList<ProductsCategory>> GetProductCategoriesByCategoryId(string categoryId, CatalogAccess access,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    ///     Inserts a mapping for product category
    /// </summary>
    /// <param name="productCategory">>Product category mapping</param>
    /// <param name="productId">Product id</param>
    Task InsertProductCategory(ProductCategory productCategory, string productId);

    /// <summary>
    ///     Updates a mapping for product category
    /// </summary>
    /// <param name="productCategory">>Product category mapping</param>
    /// <param name="productId">Product id</param>
    Task UpdateProductCategory(ProductCategory productCategory, string productId);

    /// <summary>
    ///     Deletes a mapping for product category
    /// </summary>
    /// <param name="productCategory">Product category</param>
    /// <param name="productId">Product id</param>
    Task DeleteProductCategory(ProductCategory productCategory, string productId);
}