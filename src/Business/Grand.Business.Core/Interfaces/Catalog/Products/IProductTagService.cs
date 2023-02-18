using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    /// <summary>
    /// Product tag service interface
    /// </summary>
    public interface IProductTagService
    {
       
        /// <summary>
        /// Gets all product tags
        /// </summary>
        /// <returns>Product tags</returns>
        Task<IList<ProductTag>> GetAllProductTags();

        /// <summary>
        /// Gets product tag
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Product tag</returns>
        Task<ProductTag> GetProductTagById(string productTagId);

        /// <summary>
        /// Gets product tag by name
        /// </summary>
        /// <param name="name">Product tag name</param>
        /// <returns>Product tag</returns>
        Task<ProductTag> GetProductTagByName(string name);

        /// <summary>
        /// Gets product tag by se-name
        /// </summary>
        /// <param name="sename">Product tag se-name</param>
        /// <returns>Product tag</returns>
        Task<ProductTag> GetProductTagBySeName(string sename);

        /// <summary>
        /// Inserts a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        Task InsertProductTag(ProductTag productTag);

        /// <summary>
        /// Update a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        Task UpdateProductTag(ProductTag productTag);

        /// <summary>
        /// Delete a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        Task DeleteProductTag(ProductTag productTag);

        /// <summary>
        /// Assign a tag to the product
        /// </summary>
        /// <param name="productTag">Product Tag</param>
        /// <param name="productId">Product ident</param>
        Task AttachProductTag(ProductTag productTag, string productId);

        /// <summary>
        /// Detach a tag from the product
        /// </summary>
        /// <param name="productTag">Product Tag</param>
        /// <param name="productId">Product ident</param>
        Task DetachProductTag(ProductTag productTag, string productId);

        /// <summary>
        /// Get number of products
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Number of products</returns>
        Task<int> GetProductCount(string productTagId, string storeId);
    }
}
